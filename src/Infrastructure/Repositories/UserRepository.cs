using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IApplicationDbContext _context;

    public UserRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<List<User>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }
}