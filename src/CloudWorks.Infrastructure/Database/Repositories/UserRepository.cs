using Application.Users;
using Domain.Users;
using Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Repositories;

public sealed class UserRepository(CloudWorksDbContext database) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        database
            .Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        database
            .Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);
}
