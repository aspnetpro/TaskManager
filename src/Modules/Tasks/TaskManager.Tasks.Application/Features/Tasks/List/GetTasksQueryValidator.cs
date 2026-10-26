namespace TaskManager.Tasks.Application.Features.Tasks.List;

public sealed class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, GetTasksQuery.MaxPageSize);
        RuleFor(q => q.SearchTerm).MaximumLength(GetTasksQuery.MaxSearchTermLength);
        RuleFor(q => q.Status).IsInEnum();
        RuleFor(q => q.Priority).IsInEnum();
        RuleFor(q => q.SortBy).IsInEnum();
        RuleFor(q => q.SortDirection).IsInEnum();
    }
}
