using TaskManager.Auth.Application.Features.Auth.Register;

namespace TaskManager.Auth.UnitTests.Features.Auth;

public sealed class RegisterHandlerTests
{
    private readonly AuthFixture _f = new();

    private RegisterHandler Handler() => new(_f.Users, _f.Workspaces, _f.PasswordHasher, _f.Sessions(), _f.UnitOfWork);

    private static RegisterCommand Command(string email = "Ana@Example.com") =>
        new(email, "correct-horse-battery", "Ana Souza", "Acme");

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAsWorkspaceAdmin()
    {
        Workspace? created = null;
        _f.Workspaces.Add(Arg.Do<Workspace>(w => created = w));

        var result = await Handler().Handle(Command(), CancellationToken.None);

        result.Value.User.Email.Should().Be("ana@example.com");
        result.Value.Workspace.Role.Should().Be(WorkspaceRole.Admin);
        created!.Members.Should().ContainSingle().Which.UserId.Should().Be(result.Value.User.Id);
        _f.Users.Received(1).Add(Arg.Is<User>(u => u.PasswordHash == "pbkdf2:correct-horse-battery"));
        _f.RefreshTokens.Received(1).Add(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Handle_EmailTaken_ReturnsConflictWithoutSaving()
    {
        _f.Users.EmailExistsAsync("ana@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        result.Error.Should().Be(AuthErrors.EmailAlreadyRegistered);
        await _f.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ConcurrentRegistrationHitsUniqueIndex_ReturnsConflict()
    {
        _f.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateKey));

        var result = await Handler().Handle(Command(), CancellationToken.None);

        result.Error.Should().Be(AuthErrors.EmailAlreadyRegistered);
    }
}
