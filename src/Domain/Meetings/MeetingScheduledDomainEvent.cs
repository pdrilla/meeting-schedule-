using SharedKernel;

namespace Domain.Meetings;

public sealed record MeetingScheduledDomainEvent(
    int MeetingId,
    List<int> ParticipantIds,
    DateTime StartTime,
    DateTime EndTime) : IDomainEvent;