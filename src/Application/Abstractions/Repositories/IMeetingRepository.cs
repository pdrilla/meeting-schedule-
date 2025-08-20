using Domain.Meetings;

namespace Application.Abstractions.Repositories;

public interface IMeetingRepository
{
    Task<Meeting> AddAsync(Meeting meeting, CancellationToken cancellationToken = default);
    Task<List<Meeting>> GetUserMeetingsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<Meeting>> GetConflictingMeetingsAsync(
        List<int> participantIds,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);
}