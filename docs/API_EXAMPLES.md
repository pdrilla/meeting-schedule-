# API Examples

This document provides comprehensive examples of using the Meeting Scheduler API, including common scenarios, error handling, and best practices.

## Base URL

- **Development**: `https://localhost:5001`
- **Production**: `https://your-domain.com`

## Authentication

Currently, the API does not require authentication. All endpoints are publicly accessible.

## Request Headers

### Recommended Headers

```http
Content-Type: application/json
Accept: application/json
X-Correlation-ID: your-correlation-id  # Optional, for request tracking
```

## User Management

### Create a User

Create a new user in the system.

**Request:**

```http
POST /users
Content-Type: application/json

{
  "name": "Alice Johnson"
}
```

**Success Response (201 Created):**

```json
{
  "id": 1,
  "name": "Alice Johnson"
}
```

**Error Response (400 Bad Request):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "User name cannot be empty",
  "instance": "/users"
}
```

### cURL Example

```bash
curl -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: $(uuidgen)" \
  -d '{
    "name": "Alice Johnson"
  }'
```

### PowerShell Example

```powershell
$headers = @{
    "Content-Type" = "application/json"
    "X-Correlation-ID" = [System.Guid]::NewGuid().ToString()
}

$body = @{
    name = "Alice Johnson"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/users" -Method POST -Headers $headers -Body $body
```

## Meeting Scheduling

### Schedule a Meeting

Schedule a meeting for multiple participants.

**Request:**

```http
POST /meetings
Content-Type: application/json

{
  "participantIds": [1, 2, 3],
  "durationMinutes": 60,
  "earliestStart": "2024-06-15T09:00:00Z",
  "latestEnd": "2024-06-15T17:00:00Z"
}
```

**Success Response (201 Created):**

```json
{
  "id": 1,
  "participants": [
    {
      "id": 1,
      "name": "Alice Johnson"
    },
    {
      "id": 2,
      "name": "Bob Smith"
    },
    {
      "id": 3,
      "name": "Carol Davis"
    }
  ],
  "startTime": "2024-06-15T10:00:00Z",
  "endTime": "2024-06-15T11:00:00Z"
}
```

**Error Responses:**

**Participant Not Found (404 Not Found):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The user with the Id = '999' was not found",
  "instance": "/meetings"
}
```

**No Available Slot (400 Bad Request):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "No available time slot found within the specified constraints",
  "instance": "/meetings"
}
```

### cURL Example

```bash
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: $(uuidgen)" \
  -d '{
    "participantIds": [1, 2, 3],
    "durationMinutes": 60,
    "earliestStart": "2024-06-15T09:00:00Z",
    "latestEnd": "2024-06-15T17:00:00Z"
  }'
```

### PowerShell Example

```powershell
$headers = @{
    "Content-Type" = "application/json"
    "X-Correlation-ID" = [System.Guid]::NewGuid().ToString()
}

$body = @{
    participantIds = @(1, 2, 3)
    durationMinutes = 60
    earliestStart = "2024-06-15T09:00:00Z"
    latestEnd = "2024-06-15T17:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/meetings" -Method POST -Headers $headers -Body $body
```

## Meeting Retrieval

### Get User Meetings

Retrieve all meetings for a specific user.

**Request:**

```http
GET /users/1/meetings
```

**Success Response (200 OK):**

```json
[
  {
    "id": 1,
    "participants": [
      {
        "id": 1,
        "name": "Alice Johnson"
      },
      {
        "id": 2,
        "name": "Bob Smith"
      }
    ],
    "startTime": "2024-06-15T10:00:00Z",
    "endTime": "2024-06-15T11:00:00Z"
  },
  {
    "id": 2,
    "participants": [
      {
        "id": 1,
        "name": "Alice Johnson"
      },
      {
        "id": 3,
        "name": "Carol Davis"
      }
    ],
    "startTime": "2024-06-15T14:00:00Z",
    "endTime": "2024-06-15T15:30:00Z"
  }
]
```

**Empty Response (200 OK):**

```json
[]
```

**Error Response (404 Not Found):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The user with the Id = '999' was not found",
  "instance": "/users/999/meetings"
}
```

### cURL Example

```bash
curl -X GET "https://localhost:5001/users/1/meetings" \
  -H "Accept: application/json" \
  -H "X-Correlation-ID: $(uuidgen)"
```

### PowerShell Example

```powershell
$headers = @{
    "Accept" = "application/json"
    "X-Correlation-ID" = [System.Guid]::NewGuid().ToString()
}

Invoke-RestMethod -Uri "https://localhost:5001/users/1/meetings" -Method GET -Headers $headers
```

## Common Scenarios

### Scenario 1: Create Users and Schedule Meeting

```bash
# Step 1: Create users
USER1=$(curl -s -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Alice Johnson"}' | jq -r '.id')

USER2=$(curl -s -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Bob Smith"}' | jq -r '.id')

USER3=$(curl -s -X POST "https://localhost:5001/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Carol Davis"}' | jq -r '.id')

# Step 2: Schedule meeting
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d "{
    \"participantIds\": [$USER1, $USER2, $USER3],
    \"durationMinutes\": 60,
    \"earliestStart\": \"$(date -u -d '+1 day 09:00' +%Y-%m-%dT%H:%M:%SZ)\",
    \"latestEnd\": \"$(date -u -d '+1 day 17:00' +%Y-%m-%dT%H:%M:%SZ)\"
  }"
```

### Scenario 2: Schedule Multiple Meetings

```bash
# Schedule morning meeting
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [1, 2],
    "durationMinutes": 30,
    "earliestStart": "2024-06-15T09:00:00Z",
    "latestEnd": "2024-06-15T12:00:00Z"
  }'

# Schedule afternoon meeting
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [2, 3],
    "durationMinutes": 45,
    "earliestStart": "2024-06-15T13:00:00Z",
    "latestEnd": "2024-06-15T17:00:00Z"
  }'
```

### Scenario 3: Handle Scheduling Conflicts

```bash
# First meeting (will succeed)
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [1, 2],
    "durationMinutes": 120,
    "earliestStart": "2024-06-15T10:00:00Z",
    "latestEnd": "2024-06-15T12:00:00Z"
  }'

# Second meeting (may fail due to conflict)
curl -X POST "https://localhost:5001/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [1, 3],
    "durationMinutes": 60,
    "earliestStart": "2024-06-15T10:30:00Z",
    "latestEnd": "2024-06-15T11:30:00Z"
  }'
```

## Error Handling Best Practices

### 1. Check HTTP Status Codes

Always check the HTTP status code before processing the response:

```javascript
fetch("/meetings", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-Correlation-ID": crypto.randomUUID(),
  },
  body: JSON.stringify(meetingData),
})
  .then((response) => {
    if (!response.ok) {
      return response.json().then((error) => {
        throw new Error(`${error.title}: ${error.detail}`);
      });
    }
    return response.json();
  })
  .then((meeting) => {
    console.log("Meeting scheduled:", meeting);
  })
  .catch((error) => {
    console.error("Scheduling failed:", error.message);
  });
```

### 2. Handle Specific Error Cases

```python
import requests
import json

def schedule_meeting(participant_ids, duration, earliest_start, latest_end):
    url = "https://localhost:5001/meetings"
    headers = {
        "Content-Type": "application/json",
        "X-Correlation-ID": str(uuid.uuid4())
    }
    data = {
        "participantIds": participant_ids,
        "durationMinutes": duration,
        "earliestStart": earliest_start,
        "latestEnd": latest_end
    }

    try:
        response = requests.post(url, headers=headers, json=data)

        if response.status_code == 201:
            return response.json()
        elif response.status_code == 400:
            error = response.json()
            if "No available time slot" in error.get("detail", ""):
                print("No available slots. Try different time range or fewer participants.")
            else:
                print(f"Validation error: {error.get('detail')}")
        elif response.status_code == 404:
            error = response.json()
            print(f"Participant not found: {error.get('detail')}")
        else:
            print(f"Unexpected error: {response.status_code}")

    except requests.exceptions.RequestException as e:
        print(f"Network error: {e}")

    return None
```

### 3. Retry Logic for Transient Failures

```csharp
public async Task<MeetingDto?> ScheduleMeetingWithRetry(ScheduleMeetingRequest request, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/meetings", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MeetingDto>();
            }

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound)
            {
                // Don't retry client errors
                var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                throw new InvalidOperationException(error.Detail);
            }

            // Retry server errors
            if (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
            }
        }
        catch (HttpRequestException) when (attempt < maxRetries)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }

    throw new InvalidOperationException("Failed to schedule meeting after multiple attempts");
}
```

## Performance Considerations

### 1. Use Correlation IDs

Always include correlation IDs for request tracking:

```http
X-Correlation-ID: 12345678-1234-1234-1234-123456789012
```

### 2. Monitor Response Times

Check the response time header:

```http
X-Response-Time-Ms: 150
```

### 3. Batch Operations

For multiple users, create them in parallel:

```javascript
const users = ["Alice", "Bob", "Carol", "David"];

const createUserPromises = users.map((name) =>
  fetch("/users", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name }),
  }).then((r) => r.json())
);

const createdUsers = await Promise.all(createUserPromises);
```

## Testing Examples

### Integration Test Example

```csharp
[Fact]
public async Task ScheduleMeeting_WithValidRequest_ReturnsCreatedMeeting()
{
    // Arrange
    var user1 = await CreateUserAsync("Alice");
    var user2 = await CreateUserAsync("Bob");

    var request = new ScheduleMeetingRequest
    {
        ParticipantIds = [user1.Id, user2.Id],
        DurationMinutes = 60,
        EarliestStart = DateTime.UtcNow.Date.AddHours(10),
        LatestEnd = DateTime.UtcNow.Date.AddHours(16)
    };

    // Act
    var response = await HttpClient.PostAsJsonAsync("/meetings", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var meeting = await response.Content.ReadFromJsonAsync<MeetingDto>();
    meeting.Should().NotBeNull();
    meeting.Participants.Should().HaveCount(2);
}
```

### Load Testing with curl

```bash
#!/bin/bash

# Create test users
for i in {1..10}; do
  curl -s -X POST "https://localhost:5001/users" \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"User $i\"}" > /dev/null
done

# Schedule meetings concurrently
for i in {1..50}; do
  (
    curl -s -X POST "https://localhost:5001/meetings" \
      -H "Content-Type: application/json" \
      -d '{
        "participantIds": [1, 2],
        "durationMinutes": 30,
        "earliestStart": "2024-06-15T09:00:00Z",
        "latestEnd": "2024-06-15T17:00:00Z"
      }' > /dev/null
  ) &
done

wait
echo "Load test completed"
```

## Monitoring and Debugging

### Health Check

```bash
curl -X GET "https://localhost:5001/health"
```

### Response Headers

Monitor these headers for debugging:

- `X-Correlation-ID`: Request tracking ID
- `X-Response-Time-Ms`: Request processing time
- `Content-Type`: Response format

### Seq Queries

Use these queries in Seq for monitoring:

```sql
-- Find requests by correlation ID
CorrelationId = 'your-correlation-id'

-- Monitor slow requests
RequestPath != null and ElapsedMs > 1000

-- Track scheduling failures
@Message like '%No available slot%'
```
