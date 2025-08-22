using Domain.Meetings;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    List<MeetingUser> MeetingUsers { get; }
    List<Meeting> Meetings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    int GetNextUserId();
    int GetNextMeetingId();
}
