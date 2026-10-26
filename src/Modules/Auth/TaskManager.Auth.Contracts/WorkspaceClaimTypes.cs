namespace TaskManager.Auth.Contracts;

/// <summary>
/// Claims do access token. O Auth emite, a Api lê — o nome mora aqui para
/// os dois lados não divergirem.
/// </summary>
public static class WorkspaceClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string WorkspaceId = "workspace_id";
    public const string WorkspaceRole = "workspace_role";
}
