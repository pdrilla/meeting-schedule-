using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Meetings;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

internal sealed class MeetingRepository : IMeetingRepository
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MeetingRepository> _logger;

    public MeetingRepository(IApplicationDbContext context, ILogger<MeetingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<Meeting> AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new meeting for participants {ParticipantIds} at {StartTime} - {EndTime}",
            meeting.ParticipantIds, meeting.StartTime, meeting.EndTime);

        int id = _context.GetNextMeetingId();
#pragma warning disable S3011
        typeof(Meeting).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(meeting, id);
#pragma warning restore S3011
        _context.Meetings.Add(meeting);

        _logger.LogInformation("Successfully saved meeting {MeetingId} to database", meeting.Id);
        return Task.FromResult(meeting);
    }

    public Task<List<Meeting>> GetUserMeetingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving meetings for user {UserId}", userId);

        var meetings = _context.Meetings
            .Where(m => m.ParticipantIds.Contains(userId))
            .OrderBy(m => m.StartTime)
            .ToList();

        _logger.LogDebug("Found {MeetingCount} meetings for user {UserId}", meetings.Count, userId);
        return Task.FromResult(meetings);
    }

    public Task<List<Meeting>> GetConflictingMeetingsAsync(
        List<int> participantIds,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for conflicting meetings for participants {ParticipantIds} between {StartTime} and {EndTime}",
            participantIds, start, end);

        var meetings = _context.Meetings
            .Where(m => m.StartTime < end && m.EndTime > start)
            .Where(m => m.ParticipantIds.Any(id => participantIds.Contains(id)))
            .ToList();

        _logger.LogDebug("Found {ConflictingMeetingCount} potentially conflicting meetings", meetings.Count);
        return Task.FromResult(meetings);
    }
}