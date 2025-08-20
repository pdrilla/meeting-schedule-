# Design Document

## Overview

The Meeting Scheduler system is designed as a backend API service built with ASP.NET Core using Clean Architecture principles. The system provides conflict-free meeting scheduling capabilities for multiple participants while enforcing business hours constraints. The architecture follows CQRS patterns with MediatR for command and query handling, ensuring clear separation of concerns and maintainability.

## Architecture

### Layer Structure

The system follows Clean Architecture with four distinct layers:

1. **Domain Layer** - Contains business entities, value objects, and domain services
2. **Application Layer** - Contains business logic, CQRS handlers, and application services
3. **Infrastructure Layer** - Contains data access, external services, and cross-cutting concerns
4. **API Layer** - Contains controllers/endpoints, DTOs, and API-specific concerns

### Dependency Flow

Dependencies flow inward toward the Domain layer:

- API Layer → Application Layer → Domain Layer
- Infrastructure Layer → Application Layer → Domain Layer
- No layer depends on outer layers or infrastructure details

## Components and Interfaces

### Domain Layer

#### Entities

**User Entity**

```csharp
public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; }

    // Domain methods for validation and business rules
    public void UpdateName(string name);
}
```

**Meeting Entity**

```csharp
public class Meeting
{
    public int Id { get; private set; }
    public List<User> Participants { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    // Domain invariants and business rules
    public void ValidateTimeSlot();
    public bool HasConflictWith(Meeting other);
    public bool IsWithinBusinessHours();
}
```

#### Domain Services

**MeetingSchedulerService**

```csharp
public interface IMeetingSchedulerService
{
    Task<DateTime?> FindEarliestAvailableSlot(
        List<int> participantIds,
        int durationMinutes,
        DateTime earliestStart,
        DateTime latestEnd);
}
```

### Application Layer

#### CQRS Commands

**CreateUserCommand**

```csharp
public record CreateUserCommand(string Name) : IRequest<UserDto>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    // Implementation with validation and repository interaction
}
```

**ScheduleMeetingCommand**

```csharp
public record ScheduleMeetingCommand(
    List<int> ParticipantIds,
    int DurationMinutes,
    DateTime EarliestStart,
    DateTime LatestEnd) : IRequest<MeetingDto>;

public class ScheduleMeetingCommandHandler : IRequestHandler<ScheduleMeetingCommand, MeetingDto>
{
    // Implementation using MeetingSchedulerService
}
```

#### CQRS Queries

**GetUserMeetingsQuery**

```csharp
public record GetUserMeetingsQuery(int UserId) : IRequest<List<MeetingDto>>;

public class GetUserMeetingsQueryHandler : IRequestHandler<GetUserMeetingsQuery, List<MeetingDto>>
{
    // Implementation with repository access
}
```

#### Repository Interfaces

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User> AddAsync(User user);
    Task<List<User>> GetByIdsAsync(List<int> ids);
}

public interface IMeetingRepository
{
    Task<Meeting> AddAsync(Meeting meeting);
    Task<List<Meeting>> GetUserMeetingsAsync(int userId);
    Task<List<Meeting>> GetConflictingMeetingsAsync(List<int> participantIds, DateTime start, DateTime end);
}
```

### Infrastructure Layer

#### Data Access

- Entity Framework Core for data persistence
- Repository pattern implementations
- Database context with proper entity configurations

#### External Services

- Logging with Serilog
- Structured logging with Seq integration

### API Layer

#### Endpoints

- Minimal APIs or Controllers for clean HTTP interface
- Input validation and model binding
- Proper HTTP status code handling
- DTO mapping from domain entities

## Data Models

### Database Schema

**Users Table**

- Id (Primary Key)
- Name (Required, Max Length: 100)

**Meetings Table**

- Id (Primary Key)
- StartTime (DateTime, Required)
- EndTime (DateTime, Required)

**MeetingParticipants Table** (Many-to-Many)

- MeetingId (Foreign Key)
- UserId (Foreign Key)

### DTOs

**UserDto**

```csharp
public record UserDto(int Id, string Name);
```

**MeetingDto**

```csharp
public record MeetingDto(
    int Id,
    List<UserDto> Participants,
    DateTime StartTime,
    DateTime EndTime);
```

**CreateUserRequest**

```csharp
public record CreateUserRequest(string Name);
```

**ScheduleMeetingRequest**

```csharp
public record ScheduleMeetingRequest(
    List<int> ParticipantIds,
    int DurationMinutes,
    DateTime EarliestStart,
    DateTime LatestEnd);
```

## Error Handling

### Domain Exceptions

- Custom domain exceptions for business rule violations
- Validation exceptions for invalid input data

### API Error Responses

- HTTP 400 for validation errors and bad requests
- HTTP 404 for resource not found
- HTTP 500 for unexpected server errors
- Consistent error response format with problem details

### Logging Strategy

- Structured logging with Serilog
- Request/response logging for API calls
- Error logging with correlation IDs
- Performance logging for scheduling algorithm

## Testing Strategy

### Unit Tests

- Domain entity business rules and invariants
- CQRS command and query handlers
- Meeting scheduling algorithm logic
- Repository interface mocking

### Integration Tests

- End-to-end API endpoint testing
- Database integration with test containers
- Meeting scheduling scenarios
- Error handling and edge cases

### Test Data Management

- In-memory database for unit tests
- Test data builders for consistent test setup
- Fixture patterns for complex scenarios

## Meeting Scheduling Algorithm

### Core Logic

1. **Input Validation**: Validate participant IDs, duration, and time constraints
2. **Availability Check**: Query existing meetings for all participants
3. **Slot Generation**: Generate potential time slots within business hours
4. **Conflict Detection**: Check each slot against existing meetings
5. **Selection**: Return the earliest available slot or null if none found

### Business Rules

- Business hours: 09:00-17:00 UTC
- No overlapping meetings for any participant
- Meeting duration must fit within business hours
- Back-to-back meetings are allowed (end time = start time)

### Performance Considerations

- Efficient database queries for conflict detection
- Indexing on meeting times and participant relationships
- Caching strategies for frequently accessed data

## Security Considerations

### Input Validation

- Parameter validation at API boundary
- SQL injection prevention through parameterized queries
- Input sanitization for user-provided data

### Authentication & Authorization

- Framework prepared for future authentication integration
- Role-based access control structure
- API key or JWT token support capability

## Deployment and Configuration

### Configuration Management

- Environment-specific settings via appsettings.json
- Connection strings and external service URLs
- Business hours configuration
- Logging levels and targets

### Database Management

- Entity Framework migrations for schema changes
- Seed data for development and testing
- Connection pooling and performance optimization

### Monitoring and Observability

- Health checks for database connectivity
- Metrics collection for API performance
- Structured logging for troubleshooting
- Integration with Seq for log analysis
