# Logging and Monitoring Guide

## Overview

The Meeting Scheduler system implements comprehensive structured logging and monitoring using Serilog with multiple sinks for different environments.

## Logging Architecture

### Structured Logging

The application uses Serilog for structured logging with the following features:

- **Correlation IDs**: Every request gets a unique correlation ID for tracking across services
- **Performance Monitoring**: Automatic logging of slow requests (>1000ms)
- **Contextual Information**: Rich context including user IDs, meeting details, and timing information
- **Multiple Sinks**: Console, Seq, and File logging depending on environment

### Log Levels

#### Development Environment

- **Default**: Information
- **Microsoft/System**: Warning
- **EntityFrameworkCore**: Warning
- **MeetingScheduler**: Debug

#### Production Environment

- **Default**: Warning
- **Microsoft/System**: Error
- **EntityFrameworkCore**: Error
- **MeetingScheduler**: Information

## Correlation ID Support

### Request Tracking

Every HTTP request is assigned a correlation ID that flows through the entire request pipeline:

```
X-Correlation-ID: 12345678-1234-1234-1234-123456789012
```

- **Automatic Generation**: If not provided in request headers, a new GUID is generated
- **Response Headers**: Correlation ID is returned in response headers
- **Log Context**: All log entries include the correlation ID for request tracing

### Usage

Include correlation ID in client requests:

```http
GET /users/1/meetings
X-Correlation-ID: your-correlation-id
```

## Performance Monitoring

### Request Performance

- **Automatic Timing**: All requests are automatically timed
- **Slow Request Detection**: Requests taking >1000ms are logged as warnings
- **Response Headers**: Response time is included in `X-Response-Time-Ms` header

### Scheduling Algorithm Performance

The meeting scheduling algorithm includes detailed performance logging:

```csharp
// Example log output
[10:30:15 INF] [abc123] Scheduling algorithm completed in 45ms for 3 participants
[10:30:15 INF] [abc123] Found available slot after checking 12 slots: 2024-06-15 14:00:00 - 2024-06-15 15:00:00
```

## Log Categories and Examples

### User Management

```csharp
// User creation
[10:30:15 INF] [abc123] Creating user with name: John Doe
[10:30:15 INF] [abc123] Successfully created user 123 with name: John Doe

// Validation errors
[10:30:15 WRN] [abc123] User creation failed for name '': User name cannot be empty
```

### Meeting Scheduling

```csharp
// Scheduling request
[10:30:15 INF] [abc123] Scheduling meeting for 3 participants: [1, 2, 3], Duration: 60 minutes
[10:30:15 INF] [abc123] Retrieved 5 existing meetings in 12ms
[10:30:15 INF] [abc123] Found available slot after checking 8 slots: 2024-06-15 14:00:00 - 2024-06-15 15:00:00
[10:30:15 INF] [abc123] Successfully created meeting 456 for participants [1, 2, 3]

// No availability
[10:30:15 WRN] [abc123] No available slot found after checking 24 slots for 3 participants
```

### Database Operations

```csharp
// Meeting persistence
[10:30:15 DBG] [abc123] Adding new meeting for participants [1, 2, 3] at 2024-06-15 14:00:00
[10:30:15 INF] [abc123] Successfully saved meeting 456 to database

// Conflict checking
[10:30:15 DBG] [abc123] Checking for conflicting meetings for participants [1, 2, 3] between 2024-06-15 09:00:00 and 2024-06-15 17:00:00
[10:30:15 DBG] [abc123] Found 2 potentially conflicting meetings
```

### API Endpoints

```csharp
// Successful requests
[10:30:15 INF] [abc123] Received request to create user with name: John Doe
[10:30:15 INF] [abc123] Successfully created user 123 via API

// Failed requests
[10:30:15 WRN] [abc123] Failed to create user via API: Users.EmptyName - User name cannot be empty
[10:30:15 WRN] [abc123] Failed to schedule meeting via API: Meetings.NoAvailableSlot - No available time slot found
```

## Monitoring Sinks

### Console Logging

**Development Format:**

```
[10:30:15 INF] [abc123] Successfully created user 123 with name: John Doe {"UserId": 123, "UserName": "John Doe"}
```

**Production Format:**

```
[2024-06-15 10:30:15.123 +00:00 INF] [abc123] Successfully created user 123 with name: John Doe {"UserId": 123, "UserName": "John Doe"}
```

### Seq Integration

Seq provides a web-based log analysis interface:

- **Development**: http://localhost:5341
- **Production**: http://seq:5341

#### Seq Queries

Common queries for monitoring:

```sql
-- Find all slow requests
RequestPath != null and ElapsedMs > 1000

-- Find scheduling failures
@Message like '%No available slot%'

-- Find errors by correlation ID
CorrelationId = 'abc123-def456-789'

-- Monitor scheduling algorithm performance
@Message like '%Scheduling algorithm completed%'
```

### File Logging (Production)

Production logs are written to rotating files:

- **Path**: `/app/logs/meeting-scheduler-{Date}.log`
- **Retention**: 30 days
- **Rolling**: Daily

## Health Checks

### Database Health

Monitor database connectivity:

```http
GET /health
```

Response includes:

- Database connection status
- Response time
- Overall system health

### Custom Health Checks

The system includes specific health checks for:

- **PostgreSQL Database**: Connection and query performance
- **In-Memory Database**: Availability check for development

## Monitoring Best Practices

### Log Analysis

1. **Use Correlation IDs**: Always include correlation IDs when investigating issues
2. **Monitor Performance**: Watch for slow request warnings
3. **Track Scheduling Failures**: Monitor for "No available slot" messages
4. **Database Performance**: Watch database query timing logs

### Alerting Recommendations

Set up alerts for:

- **Error Rate**: >5% error rate over 5 minutes
- **Slow Requests**: >10 requests taking >2 seconds in 5 minutes
- **Scheduling Failures**: >20% scheduling failure rate
- **Database Issues**: Database health check failures

### Troubleshooting

#### Common Issues

1. **High Scheduling Times**

   - Look for "Scheduling algorithm completed" logs
   - Check participant count and existing meeting volume
   - Monitor database query performance

2. **Frequent Scheduling Failures**

   - Analyze "No available slot" patterns
   - Check business hours configuration
   - Review participant availability patterns

3. **Database Performance**
   - Monitor "Retrieved X existing meetings" timing
   - Check index usage and query plans
   - Review connection pool settings

#### Log Correlation

Use correlation IDs to trace requests:

```bash
# Find all logs for a specific request
grep "abc123-def456-789" /app/logs/meeting-scheduler-*.log

# In Seq
CorrelationId = 'abc123-def456-789'
```

## Configuration

### Environment Variables

```bash
# Seq configuration
SEQ_SERVER_URL=http://seq:5341
SEQ_API_KEY=your-api-key

# Log levels
SERILOG__MINIMUMLEVEL__DEFAULT=Information
SERILOG__MINIMUMLEVEL__OVERRIDE__APPLICATION_MEETINGSCHEDULER=Debug
```

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Application.MeetingScheduler": "Debug"
      }
    },
    "Properties": {
      "Application": "MeetingScheduler",
      "Environment": "Production"
    }
  }
}
```

## Security Considerations

### Log Sanitization

- **No Sensitive Data**: Logs do not contain passwords or tokens
- **User Data**: Only user IDs and names are logged (no PII)
- **Request Data**: Meeting times and participant IDs only

### Access Control

- **Seq Access**: Restrict access to authorized personnel
- **Log Files**: Secure file system permissions
- **Network**: Use HTTPS for Seq in production

## Performance Impact

### Logging Overhead

- **Structured Logging**: Minimal performance impact (<1ms per request)
- **Async Logging**: Non-blocking log writes
- **Buffering**: Efficient batching for high-throughput scenarios

### Resource Usage

- **Memory**: ~50MB additional for logging infrastructure
- **Disk**: Rotating logs with 30-day retention
- **Network**: Minimal for Seq communication
