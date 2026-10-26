namespace TaskManager.Auth.Application.Features.Members.List;

/// <summary>Membros do workspace atual — alimenta o seletor de responsável (US-006).</summary>
public sealed record ListMembersQuery : IRequest<Result<IReadOnlyList<MemberResponse>>>;
