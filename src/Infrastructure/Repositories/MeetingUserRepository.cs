using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Meetings;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class MeetingUserRepository : IMeetingUserRepository
{
    private readonly IApplicationDbContext _context;

    public MeetingUserRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MeetingUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.MeetingUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<MeetingUser> AddAsync(MeetingUser user, CancellationToken cancellationToken = default)
    {
        _context.MeetingUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<List<MeetingUser>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        return await _context.MeetingUsers
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }
}