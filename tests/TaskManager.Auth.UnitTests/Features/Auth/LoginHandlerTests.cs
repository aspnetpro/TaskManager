using TaskManager.Auth.Application.Features.Auth.Login;

namespace TaskManager.Auth.UnitTests.Features.Auth;

public sealed class LoginHandlerTests
{
    private const string Password = "correct-horse-battery";

    private readonly AuthFixture _f = new();
    private readonly User _user;

    public LoginHandlerTests()
    {
        _user = _f.AUser();
        _f.Users.GetByEmailAsync("ana@example.com", Arg.Any<CancellationToken>()).Returns(_user);
        _f.PasswordHasher.Verify(_user.PasswordHash, Password).Returns(PasswordVerification.Success);
    }

    private LoginHandler Handler() => new(_f.Users, _f.Workspaces, _f.PasswordHasher, _f.Sessions(), _f.UnitOfWork);

    [Fact]
    public async Task Handle_ValidCredentials_StartsSessionInOldestWorkspace()
    {
        var oldest = _f.MemberOf(Guid.NewGuid(), WorkspaceRole.TechLead);
        _f.WithMemberships(_user.Id, oldest, _f.MemberOf(Guid.NewGuid()));

        var result = await Handler().Handle(new LoginCommand(" ANA@example.com ", Password), CancellationToken.None);

        result.Value.Workspace.Should().Be(oldest);
        result.Value.RefreshToken.Should().Be("refresh-1");
        _f.RefreshTokens.Received(1).Add(Arg.Is<RefreshToken>(t => t.TokenHash == "hash:refresh-1" && t.WorkspaceId == oldest.WorkspaceId));
    }

    [Fact]
    public async Task Handle_RequestedWorkspace_UsesIt()
    {
        var requested = _f.MemberOf(Guid.NewGuid(), WorkspaceRole.Viewer);
        _f.WithMemberships(_user.Id, _f.MemberOf(Guid.NewGuid()), requested);

        var result = await Handler().Handle(new LoginCommand(_user.Email, Password, requested.WorkspaceId), CancellationToken.None);

        result.Value.Workspace.Should().Be(requested);
    }

    [Fact]
    public async Task Handle_WorkspaceUserIsNotMemberOf_ReturnsNoWorkspaceAccess()
    {
        _f.WithMemberships(_user.Id, _f.MemberOf(Guid.NewGuid()));

        var result = await Handler().Handle(new LoginCommand(_user.Email, Password, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(AuthErrors.NoWorkspaceAccess);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var result = await Handler().Handle(new LoginCommand(_user.Email, "wrong"), CancellationToken.None);

        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        _f.RefreshTokens.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_UnknownEmail_StillVerifiesPasswordAndReturnsSameError()
    {
        var result = await Handler().Handle(new LoginCommand("nobody@example.com", Password), CancellationToken.None);

        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        _f.PasswordHasher.Received(1).Verify(null, Password);
    }

    [Fact]
    public async Task Handle_RehashNeeded_UpdatesStoredHash()
    {
        _f.PasswordHasher.Verify(_user.PasswordHash, Password).Returns(PasswordVerification.SuccessRehashNeeded);
        _f.WithMemberships(_user.Id, _f.MemberOf(Guid.NewGuid()));

        await Handler().Handle(new LoginCommand(_user.Email, Password), CancellationToken.None);

        _user.PasswordHash.Should().Be($"pbkdf2:{Password}");
    }
}
