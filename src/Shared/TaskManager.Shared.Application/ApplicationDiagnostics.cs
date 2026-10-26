using System.Diagnostics;

namespace TaskManager.Shared.Application;

/// <summary>ActivitySource dos casos de uso. A Api assina pelo nome (AddSource).</summary>
public static class ApplicationDiagnostics
{
    public const string SourceName = "TaskManager.Application";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
