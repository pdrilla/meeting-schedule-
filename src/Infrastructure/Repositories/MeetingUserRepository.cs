using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Meetings;

namespace Infrastructure.Repositories;

internal sealed class MeetingUserRepository : IMeetingUserRepository
{
    private readonly IApplicationDbContext _context;

    public MeetingUserRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<MeetingUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        MeetingUser? user = _context.MeetingUsers.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<MeetingUser> AddAsync(MeetingUser user, CancellationToken cancellationToken = default)
    {
        int id = _context.GetNextUserId();
#pragma warning disable S3011
        typeof(MeetingUser).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(user, id);
#pragma warning restore S3011
        _context.MeetingUsers.Add(user);
        return Task.FromResult(user);
    }

    public Task<List<MeetingUser>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        var users = _context.MeetingUsers.Where(u => ids.Contains(u.Id)).ToList();
        return Task.FromResult(users);
    }
}