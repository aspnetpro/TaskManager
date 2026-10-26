namespace TaskManager.Shared.Domain;

/// <summary>Token de concorrência otimista, incrementado a cada update.</summary>
public interface IVersioned
{
    uint Version { get; }
}
