using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence;

internal sealed class UserRepository(AuthDbContext context) : IUserRepository
{
    public void Add(User user) => context.Users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
}
