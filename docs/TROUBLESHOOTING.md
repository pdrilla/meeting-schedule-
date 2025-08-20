# Troubleshooting Guide

This guide helps you diagnose and resolve common issues with the Meeting Scheduler API.

## Quick Diagnostics

### Health Check

First, verify the system is running:

```bash
curl -X GET "https://localhost:5001/health"
```

**Expected Response:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "meeting-scheduler-inmemory": {
      "status": "Healthy",
      "description": "In-memory database is available"
    }
  }
}
```

### Check Logs

View real-time logs in Seq: http://localhost:5341

Or check console output for immediate feedback.

## Common Issues

### 1. Application Won't Start

#### Symptoms

- Application fails to start
- Port binding errors
- Configuration errors

#### Solutions

**Port Already in Use:**

```bash
# Check what's using the port
netstat -tulpn | grep :5001

# Kill the process or use a different port
dotnet run --project src/Web.Api --urls "https://localhost:5002"
```

**Missing Dependencies:**

```bash
# Restore NuGet packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

**Configuration Issues:**

```bash
# Check appsettings.json syntax
cat src/Web.Api/appsettings.json | jq .

# Verify environment variables
env | grep -i serilog
```

### 2. Database Connection Issues

#### Symptoms

- Health check fails
- Database-related errors in logs
- Entity Framework exceptions

#### Solutions

**In-Memory Database (Development):**

```bash
# Verify no connection string is set
grep -r "ConnectionStrings" src/Web.Api/appsettings.json
```

**PostgreSQL (Production):**

```bash
# Test database connection
psql -h localhost -U your_user -d MeetingScheduler -c "SELECT 1;"

# Check connection string format
# Correct: "Host=localhost;Database=MeetingScheduler;Username=user;Password=pass"
```

**Migration Issues:**

```bash
# Apply migrations manually
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api

# Create new migration if needed
dotnet ef migrations add FixIssue --project src/Infrastructure --startup-project src/Web.Api
```

### 3. Meeting Scheduling Failures

#### Symptoms

- "No available time slot found" errors
- Unexpected scheduling behavior
- Performance issues with scheduling

#### Diagnostic Steps

**Check Business Hours:**

```bash
# Verify time zone (system uses UTC)
date -u

# Business hours are 09:00-17:00 UTC
# Convert your local time to UTC for testing
```

**Verify Participants Exist:**

```bash
# Check if users exist
curl -X GET "https://localhost:5001/users/1/meetings"

# Create test users if needed
curl -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test User"}'
```

**Check Existing Meetings:**

```bash
# Get user's existing meetings
curl -X GET "https://localhost:5001/users/1/meetings"

# Look for conflicts in the time range
```

**Monitor Scheduling Performance:**

In Seq, use this query:

```sql
@Message like '%Scheduling algorithm completed%'
| select @Timestamp, ElapsedMs, ParticipantCount
| order by @Timestamp desc
```

#### Common Scheduling Issues

**Issue: No Available Slots**

```json
{
  "detail": "No available time slot found within the specified constraints"
}
```

**Solutions:**

1. Expand time range
2. Reduce meeting duration
3. Check participant availability
4. Verify business hours alignment

**Issue: Participant Not Found**

```json
{
  "detail": "The user with the Id = '999' was not found"
}
```

**Solutions:**

1. Verify participant IDs exist
2. Create missing users
3. Check for typos in participant IDs

**Issue: Invalid Time Range**

```json
{
  "detail": "Meeting start time must be before end time"
}
```

**Solutions:**

1. Check date/time format (ISO 8601)
2. Verify time zone (use UTC)
3. Ensure earliestStart < latestEnd

### 4. Performance Issues

#### Symptoms

- Slow API responses
- High memory usage
- Database query timeouts

#### Diagnostic Steps

**Monitor Response Times:**

```bash
# Check response time headers
curl -I "https://localhost:5001/health"
# Look for: X-Response-Time-Ms: 150
```

**Check Seq for Slow Requests:**

```sql
RequestPath != null and ElapsedMs > 1000
| select @Timestamp, RequestPath, ElapsedMs, StatusCode
| order by ElapsedMs desc
```

**Database Performance:**

```sql
@Message like '%Retrieved % existing meetings%'
| extract MeetingCount, ElapsedMs from @Message
| where ElapsedMs > 100
```

#### Solutions

**Optimize Database Queries:**

```bash
# Check if indexes are being used
# Look for slow query logs in Seq

# Consider adding more indexes for specific query patterns
```

**Reduce Scheduling Complexity:**

- Limit participant count for large meetings
- Use shorter time ranges when possible
- Consider caching for frequently accessed data

**Memory Issues:**

```bash
# Monitor memory usage
dotnet-counters monitor --process-id $(pgrep -f "Web.Api")

# Check for memory leaks in logs
grep -i "memory\|gc" logs/meeting-scheduler-*.log
```

### 5. Logging and Monitoring Issues

#### Symptoms

- Missing logs
- Seq not receiving logs
- Correlation ID issues

#### Solutions

**Seq Connection Issues:**

```bash
# Test Seq connectivity
curl -X GET "http://localhost:5341/api"

# Check Seq configuration
grep -A 10 "Seq" src/Web.Api/appsettings.json

# Restart Seq container
docker restart seq
```

**Missing Correlation IDs:**

```bash
# Verify middleware is registered
grep -n "UseCorrelationId" src/Web.Api/Program.cs

# Check middleware order (should be early in pipeline)
```

**Log Level Issues:**

```json
// Adjust log levels in appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug", // Increase verbosity
      "Override": {
        "Application.MeetingScheduler": "Debug"
      }
    }
  }
}
```

### 6. API Client Issues

#### Symptoms

- HTTP 415 Unsupported Media Type
- HTTP 400 Bad Request with validation errors
- Connection refused errors

#### Solutions

**Content-Type Issues:**

```bash
# Always include Content-Type header
curl -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test User"}'
```

**JSON Format Issues:**

```bash
# Validate JSON syntax
echo '{"name": "Test User"}' | jq .

# Check for common issues:
# - Missing quotes around strings
# - Trailing commas
# - Incorrect date formats
```

**SSL/TLS Issues:**

```bash
# For development, bypass SSL validation
curl -k -X GET "https://localhost:5001/health"

# Or use HTTP endpoint
curl -X GET "http://localhost:5000/health"
```

## Error Code Reference

### HTTP Status Codes

| Code | Meaning                | Common Causes                                             |
| ---- | ---------------------- | --------------------------------------------------------- |
| 400  | Bad Request            | Invalid JSON, validation errors, business rule violations |
| 404  | Not Found              | User doesn't exist, invalid endpoint                      |
| 415  | Unsupported Media Type | Missing Content-Type header                               |
| 500  | Internal Server Error  | Unhandled exceptions, database issues                     |

### Application Error Codes

| Error Code                      | Description                      | Solution                          |
| ------------------------------- | -------------------------------- | --------------------------------- |
| `Users.NotFound`                | User with specified ID not found | Create user or verify ID          |
| `Users.EmptyName`               | User name is empty or null       | Provide valid name                |
| `Users.NameTooLong`             | User name exceeds 100 characters | Shorten name                      |
| `Meetings.NoAvailableSlot`      | No time slot available           | Adjust time range or participants |
| `Meetings.InvalidTimeSlot`      | Invalid start/end times          | Check time format and logic       |
| `Meetings.OutsideBusinessHours` | Meeting outside 09:00-17:00 UTC  | Adjust to business hours          |
| `Meetings.NoParticipants`       | No participants specified        | Add participant IDs               |

## Debugging Techniques

### 1. Enable Detailed Logging

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore": "Information",
        "Application.MeetingScheduler": "Debug"
      }
    }
  }
}
```

### 2. Use Correlation IDs

Always include correlation IDs in requests:

```bash
CORRELATION_ID=$(uuidgen)
curl -X POST "https://localhost:5001/meetings" \
  -H "X-Correlation-ID: $CORRELATION_ID" \
  -H "Content-Type: application/json" \
  -d '{"participantIds": [1, 2], "durationMinutes": 60, "earliestStart": "2024-06-15T09:00:00Z", "latestEnd": "2024-06-15T17:00:00Z"}'

# Then search logs by correlation ID
echo "Search Seq for: CorrelationId = '$CORRELATION_ID'"
```

### 3. Test with Minimal Data

```bash
# Create minimal test scenario
curl -X POST "https://localhost:5001/users" -H "Content-Type: application/json" -d '{"name": "Test1"}'
curl -X POST "https://localhost:5001/users" -H "Content-Type: application/json" -d '{"name": "Test2"}'

# Schedule simple meeting
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [1, 2],
    "durationMinutes": 30,
    "earliestStart": "2024-06-15T10:00:00Z",
    "latestEnd": "2024-06-15T11:00:00Z"
  }'
```

### 4. Database Inspection

```bash
# For development (in-memory database)
# Add breakpoints in repository methods to inspect data

# For production (PostgreSQL)
psql -h localhost -U your_user -d MeetingScheduler

-- Check users
SELECT * FROM meeting_users;

-- Check meetings
SELECT * FROM meetings;

-- Check for conflicts
SELECT * FROM meetings
WHERE start_time < '2024-06-15 11:00:00+00'
  AND end_time > '2024-06-15 10:00:00+00';
```

## Getting Help

### 1. Collect Information

Before seeking help, collect:

- Correlation ID from the failing request
- Complete error message and stack trace
- Request/response examples
- Relevant log entries from Seq
- System configuration (OS, .NET version, etc.)

### 2. Search Logs

Use Seq queries to find related issues:

```sql
-- Find all logs for a correlation ID
CorrelationId = 'your-correlation-id'

-- Find similar errors
@Message like '%your error message%'

-- Find performance issues
ElapsedMs > 1000 and @Timestamp > now() - 1h
```

### 3. Create Minimal Reproduction

Create the smallest possible example that reproduces the issue:

```bash
#!/bin/bash
# minimal-repro.sh

# Clean state
curl -X DELETE "https://localhost:5001/reset" # If available

# Create users
USER1=$(curl -s -X POST "https://localhost:5001/users" -H "Content-Type: application/json" -d '{"name": "User1"}' | jq -r '.id')
USER2=$(curl -s -X POST "https://localhost:5001/users" -H "Content-Type: application/json" -d '{"name": "User2"}' | jq -r '.id')

# Reproduce issue
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: repro-$(date +%s)" \
  -d "{
    \"participantIds\": [$USER1, $USER2],
    \"durationMinutes\": 60,
    \"earliestStart\": \"$(date -u -d 'tomorrow 09:00' +%Y-%m-%dT%H:%M:%SZ)\",
    \"latestEnd\": \"$(date -u -d 'tomorrow 17:00' +%Y-%m-%dT%H:%M:%SZ)\"
  }"
```

### 4. Check Known Issues

Review the project's issue tracker for similar problems and their solutions.

## Prevention

### 1. Input Validation

Always validate inputs on the client side:

```javascript
function validateMeetingRequest(request) {
  if (!request.participantIds || request.participantIds.length === 0) {
    throw new Error("At least one participant is required");
  }

  if (request.durationMinutes <= 0) {
    throw new Error("Duration must be positive");
  }

  if (new Date(request.earliestStart) >= new Date(request.latestEnd)) {
    throw new Error("Earliest start must be before latest end");
  }

  // Check business hours (09:00-17:00 UTC)
  const start = new Date(request.earliestStart);
  const end = new Date(request.latestEnd);

  if (start.getUTCHours() < 9 || end.getUTCHours() > 17) {
    throw new Error("Meeting must be within business hours (09:00-17:00 UTC)");
  }
}
```

### 2. Error Handling

Implement robust error handling:

```csharp
public async Task<ApiResponse<MeetingDto>> ScheduleMeetingAsync(ScheduleMeetingRequest request)
{
    try
    {
        var response = await httpClient.PostAsJsonAsync("/meetings", request);

        if (response.IsSuccessStatusCode)
        {
            var meeting = await response.Content.ReadFromJsonAsync<MeetingDto>();
            return ApiResponse<MeetingDto>.Success(meeting);
        }

        var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return ApiResponse<MeetingDto>.Failure(error.Detail);
    }
    catch (HttpRequestException ex)
    {
        return ApiResponse<MeetingDto>.Failure($"Network error: {ex.Message}");
    }
    catch (TaskCanceledException ex)
    {
        return ApiResponse<MeetingDto>.Failure("Request timeout");
    }
}
```

### 3. Monitoring

Set up proactive monitoring:

- Health check alerts
- Performance threshold alerts
- Error rate monitoring
- Capacity planning based on usage patterns
