using TaskManager.Auth.Application.Features.Auth.Refresh;

namespace TaskManager.Auth.UnitTests.Features.Auth;

public sealed class RefreshSessionHandlerTests
{
    private readonly AuthFixture _f = new();
    private readonly User _user;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly RefreshToken _current;

    public RefreshSessionHandlerTests()
    {
        _user = _f.AUser();
        _current = RefreshToken.StartFamily(_user.Id, _workspaceId, "hash:presented", _f.Clock.GetUtcNow(), AuthFixture.RefreshLifetime);
        _f.RefreshTokens.GetByHashAsync("hash:presented", Arg.Any<CancellationToken>()).Returns(_current);
        _f.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _f.WithMemberships(_user.Id, _f.MemberOf(_workspaceId, WorkspaceRole.TechLead));
    }

    private RefreshSessionHandler Handler() =>
        new(_f.RefreshTokens, _f.Users, _f.Workspaces, _f.Tokens, _f.Sessions(), _f.UnitOfWork, _f.Clock);

    private Task<Result<AuthResult>> Refresh(string token = "presented") =>
        Handler().Handle(new RefreshSessionCommand(token), CancellationToken.None);

    [Fact]
    public async Task Handle_ActiveToken_RotatesWithFreshRole()
    {
        var result = await Refresh();

        result.Value.Workspace.Role.Should().Be(WorkspaceRole.TechLead);
        result.Value.RefreshToken.Should().Be("refresh-1");
        _current.IsRevoked.Should().BeTrue();
        _f.RefreshTokens.Received(1).Add(Arg.Is<RefreshToken>(t => t.FamilyId == _current.FamilyId));
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsInvalidRefreshToken()
    {
        var result = await Refresh("forged");

        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_AlreadyRotatedToken_RevokesFamily()
    {
        _current.Rotate("hash:next", _f.Clock.GetUtcNow(), AuthFixture.RefreshLifetime);

        var result = await Refresh();

        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
        await _f.RefreshTokens.Received(1).RevokeFamilyAsync(_current.FamilyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsInvalidWithoutRevoking()
    {
        _f.Clock.Advance(AuthFixture.RefreshLifetime + TimeSpan.FromSeconds(1));

        var result = await Refresh();

        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
        await _f.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeFamilyAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_MemberRemovedFromWorkspace_RevokesFamily()
    {
        _f.WithMemberships(_user.Id);

        var result = await Refresh();

        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
        await _f.RefreshTokens.Received(1).RevokeFamilyAsync(_current.FamilyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrentRefreshLosesRace_ReturnsInvalidRefreshToken()
    {
        _f.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await Refresh();

        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }
}
