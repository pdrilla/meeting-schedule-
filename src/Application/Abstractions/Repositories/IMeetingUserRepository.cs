using Domain.Meetings;

namespace Application.Abstractions.Repositories;

public interface IMeetingUserRepository
{
    Task<MeetingUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MeetingUser> AddAsync(MeetingUser user, CancellationToken cancellationToken = default);
    Task<List<MeetingUser>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default);
}