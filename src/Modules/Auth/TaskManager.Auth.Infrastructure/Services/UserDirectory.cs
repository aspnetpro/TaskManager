using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Contracts;
using TaskManager.Auth.Infrastructure.Persistence;

namespace TaskManager.Auth.Infrastructure.Services;

internal sealed class UserDirectory(AuthDbContext context) : IUserDirectory
{
    public Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserContact(u.Id, u.Email, u.FullName))
            .FirstOrDefaultAsync(cancellationToken);
}
