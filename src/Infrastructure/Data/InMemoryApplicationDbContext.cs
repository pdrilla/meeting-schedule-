using Application.Abstractions.Data;
using Domain.Meetings;

namespace Infrastructure.Data;

public sealed class InMemoryApplicationDbContext : IApplicationDbContext
{
    private int _nextUserId = 1;
    private int _nextMeetingId = 1;

    public List<MeetingUser> MeetingUsers { get; } = [];
    public List<Meeting> Meetings { get; } = [];

    public int GetNextUserId() => _nextUserId++;

    public int GetNextMeetingId() => _nextMeetingId++;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
