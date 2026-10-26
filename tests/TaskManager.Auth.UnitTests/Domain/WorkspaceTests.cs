namespace TaskManager.Auth.UnitTests.Domain;

public sealed class WorkspaceTests
{
    [Fact]
    public void AddMember_NewUser_AddsActiveMember()
    {
        var workspace = Workspace.Create("Acme");
        var userId = Guid.NewGuid();

        var result = workspace.AddMember(userId, WorkspaceRole.TechLead);

        result.Value.Should().BeEquivalentTo(new { UserId = userId, Role = WorkspaceRole.TechLead, IsActive = true });
    }

    [Fact]
    public void AddMember_ExistingUser_ReturnsAlreadyMember()
    {
        var workspace = Workspace.Create("Acme");
        var userId = Guid.NewGuid();
        workspace.AddMember(userId, WorkspaceRole.Developer);

        var result = workspace.AddMember(userId, WorkspaceRole.Admin);

        result.Error.Should().Be(AuthErrors.AlreadyMember);
        workspace.Members.Should().ContainSingle();
    }

    [Fact]
    public void UserCreate_NormalizesEmail()
    {
        var user = User.Create("  Ana@Example.COM ", " Ana ", "hash");

        user.Email.Should().Be("ana@example.com");
        user.FullName.Should().Be("Ana");
    }
}
