# Seq Monitoring Queries

## Dashboard Queries for Meeting Scheduler

### Performance Monitoring

#### Slow Requests

```sql
RequestPath != null and ElapsedMs > 1000
| select @Timestamp, RequestPath, ElapsedMs, StatusCode, CorrelationId
| order by @Timestamp desc
```

#### Average Response Times

```sql
RequestPath != null and ElapsedMs != null
| summarize avg(ElapsedMs) by bin(@Timestamp, 5m)
| render timechart
```

#### Request Volume

```sql
RequestPath != null
| summarize count() by bin(@Timestamp, 1m), RequestPath
| render timechart
```

### Meeting Scheduler Specific

#### Scheduling Algorithm Performance

```sql
@Message like '%Scheduling algorithm completed%'
| extract ElapsedMs from @Message
| select @Timestamp, ElapsedMs, ParticipantCount
| order by @Timestamp desc
```

#### Scheduling Success Rate

```sql
(@Message like '%Successfully created meeting%' or @Message like '%No available slot%')
| extend Success = @Message like '%Successfully created meeting%'
| summarize SuccessCount = countif(Success), TotalCount = count() by bin(@Timestamp, 5m)
| extend SuccessRate = SuccessCount * 100.0 / TotalCount
| render timechart
```

#### Failed Scheduling Attempts

```sql
@Message like '%No available slot%'
| select @Timestamp, ParticipantIds, Duration, CorrelationId
| order by @Timestamp desc
```

### Error Monitoring

#### Application Errors

```sql
@Level = 'Error'
| select @Timestamp, @Message, @Exception, CorrelationId
| order by @Timestamp desc
```

#### Validation Errors

```sql
@Level = 'Warning' and (@Message like '%failed%' or @Message like '%invalid%')
| select @Timestamp, @Message, CorrelationId
| order by @Timestamp desc
```

#### Database Issues

```sql
@Message like '%database%' and @Level in ['Warning', 'Error']
| select @Timestamp, @Message, @Exception
| order by @Timestamp desc
```

### User Activity

#### User Creation Activity

```sql
@Message like '%Creating user%' or @Message like '%Successfully created user%'
| select @Timestamp, UserName, UserId, CorrelationId
| order by @Timestamp desc
```

#### Meeting Creation Activity

```sql
@Message like '%Scheduling meeting%' or @Message like '%Successfully created meeting%'
| select @Timestamp, ParticipantIds, Duration, MeetingId, CorrelationId
| order by @Timestamp desc
```

### System Health

#### Health Check Status

```sql
RequestPath = '/health'
| select @Timestamp, StatusCode, ElapsedMs
| order by @Timestamp desc
```

#### Database Performance

```sql
@Message like '%Retrieved % existing meetings%'
| extract MeetingCount, ElapsedMs from @Message
| select @Timestamp, MeetingCount, ElapsedMs
| order by @Timestamp desc
```

## Alert Queries

### Critical Alerts

#### High Error Rate (>5% in 5 minutes)

```sql
@Level = 'Error'
| where @Timestamp > now() - 5m
| summarize ErrorCount = count()
| where ErrorCount > 10
```

#### Database Connection Issues

```sql
@Message like '%database%' and @Level = 'Error'
| where @Timestamp > now() - 1m
```

#### Scheduling Algorithm Timeout

```sql
@Message like '%Scheduling algorithm completed%' and ElapsedMs > 5000
| where @Timestamp > now() - 5m
```

### Warning Alerts

#### High Scheduling Failure Rate (>20% in 10 minutes)

```sql
(@Message like '%Successfully created meeting%' or @Message like '%No available slot%')
| where @Timestamp > now() - 10m
| extend Success = @Message like '%Successfully created meeting%'
| summarize SuccessCount = countif(Success), TotalCount = count()
| extend FailureRate = (TotalCount - SuccessCount) * 100.0 / TotalCount
| where FailureRate > 20
```

#### Slow Scheduling Algorithm (>2 seconds)

```sql
@Message like '%Scheduling algorithm completed%' and ElapsedMs > 2000
| where @Timestamp > now() - 5m
```

## Saved Searches

### Daily Summary

```sql
@Timestamp > now() - 1d
| extend Category = case(
    @Message like '%Successfully created user%', 'User Created',
    @Message like '%Successfully created meeting%', 'Meeting Created',
    @Message like '%No available slot%', 'Scheduling Failed',
    @Level = 'Error', 'Error',
    'Other'
)
| summarize count() by Category
```

### Performance Summary

```sql
RequestPath != null and ElapsedMs != null
| where @Timestamp > now() - 1h
| summarize
    AvgResponseTime = avg(ElapsedMs),
    MaxResponseTime = max(ElapsedMs),
    RequestCount = count(),
    SlowRequests = countif(ElapsedMs > 1000)
by RequestPath
```

### Top Correlation IDs by Activity

```sql
CorrelationId != null
| where @Timestamp > now() - 1h
| summarize LogCount = count() by CorrelationId
| order by LogCount desc
| take 20
```
