namespace TaskManager.Shared.Domain;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
}
