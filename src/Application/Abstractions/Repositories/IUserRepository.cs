using Domain.Users;

namespace Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default);
}