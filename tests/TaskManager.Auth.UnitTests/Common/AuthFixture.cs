namespace TaskManager.Auth.UnitTests.Common;

/// <summary>Dublês comuns dos handlers do Auth. Token "hash" é previsível: hash:{valor}.</summary>
public sealed class AuthFixture
{
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

    private int _generated;

    public AuthFixture()
    {
        Tokens.RefreshTokenLifetime.Returns(RefreshLifetime);
        Tokens.HashRefreshToken(Arg.Any<string>()).Returns(call => $"hash:{call.Arg<string>()}");
        Tokens.GenerateRefreshToken().Returns(_ =>
        {
            var value = $"refresh-{++_generated}";
            return new GeneratedRefreshToken(value, $"hash:{value}");
        });
        Tokens.CreateAccessToken(Arg.Any<User>(), Arg.Any<MembershipSummary>(), Arg.Any<DateTimeOffset>())
            .Returns(call => new AccessToken("access-token", call.Arg<DateTimeOffset>().AddMinutes(15)));

        UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        PasswordHasher.Hash(Arg.Any<string>()).Returns(call => $"pbkdf2:{call.Arg<string>()}");
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero));
    public ITokenService Tokens { get; } = Substitute.For<ITokenService>();
    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();
    public IWorkspaceRepository Workspaces { get; } = Substitute.For<IWorkspaceRepository>();
    public IRefreshTokenRepository RefreshTokens { get; } = Substitute.For<IRefreshTokenRepository>();
    public IPasswordHasher PasswordHasher { get; } = Substitute.For<IPasswordHasher>();
    public IAuthUnitOfWork UnitOfWork { get; } = Substitute.For<IAuthUnitOfWork>();

    public SessionIssuer Sessions() => new(Tokens, RefreshTokens, Clock);

    public User AUser(string email = "ana@example.com") => User.Create(email, "Ana Souza", "pbkdf2:stored");

    public MembershipSummary MemberOf(Guid workspaceId, WorkspaceRole role = WorkspaceRole.Developer) =>
        new(workspaceId, "Acme", role);

    public void WithMemberships(Guid userId, params MembershipSummary[] memberships) =>
        Workspaces.GetActiveMembershipsAsync(userId, Arg.Any<CancellationToken>()).Returns(memberships);
}
