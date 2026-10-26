namespace TaskManager.Auth.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    [Fact]
    public void StartFamily_NewToken_IsActiveUntilExpiration()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Lifetime);

        token.IsActive(Now).Should().BeTrue();
        token.IsActive(Now + Lifetime).Should().BeFalse();
        token.ExpiresAt.Should().Be(Now + Lifetime);
    }

    [Fact]
    public void Rotate_ActiveToken_RevokesItAndKeepsFamily()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), Guid.NewGuid(), "old", Now, Lifetime);
        var later = Now.AddHours(1);

        var next = token.Rotate("new", later, Lifetime);

        token.IsRevoked.Should().BeTrue();
        token.ReplacedByTokenId.Should().Be(next.Id);
        next.FamilyId.Should().Be(token.FamilyId);
        next.UserId.Should().Be(token.UserId);
        next.WorkspaceId.Should().Be(token.WorkspaceId);
        next.ExpiresAt.Should().Be(later + Lifetime);
    }

    [Fact]
    public void Rotate_RevokedToken_Throws()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), Guid.NewGuid(), "old", Now, Lifetime);
        token.Rotate("new", Now, Lifetime);

        var act = () => token.Rotate("again", Now, Lifetime);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartFamily_TwoSessions_GetDifferentFamilies()
    {
        var userId = Guid.NewGuid();

        var a = RefreshToken.StartFamily(userId, Guid.NewGuid(), "a", Now, Lifetime);
        var b = RefreshToken.StartFamily(userId, Guid.NewGuid(), "b", Now, Lifetime);

        a.FamilyId.Should().NotBe(b.FamilyId);
    }
}
