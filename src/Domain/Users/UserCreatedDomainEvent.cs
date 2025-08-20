using SharedKernel;

namespace Domain.Users;

public sealed record UserCreatedDomainEvent(int UserId, string Name) : IDomainEvent;