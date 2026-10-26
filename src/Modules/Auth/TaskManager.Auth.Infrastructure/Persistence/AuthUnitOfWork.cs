using TaskManager.Auth.Application.Abstractions;
using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Auth.Infrastructure.Persistence;

internal sealed class AuthUnitOfWork(AuthDbContext context)
    : EfUnitOfWork<AuthDbContext>(context), IAuthUnitOfWork;
