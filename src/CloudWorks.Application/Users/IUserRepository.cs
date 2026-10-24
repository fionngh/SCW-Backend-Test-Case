using Domain.Users;

namespace Application.Users;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}
