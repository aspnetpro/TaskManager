using TaskManager.Shared.Domain;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
