using SharedKernel;

namespace Domain.Meetings;

public sealed record MeetingUserCreatedDomainEvent(int UserId, string Name) : IDomainEvent;