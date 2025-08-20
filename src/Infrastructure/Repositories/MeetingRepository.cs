using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Meetings;
using Microsoft.EntityFrameworkCore;
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

    public async Task<Meeting> AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new meeting for participants {ParticipantIds} at {StartTime} - {EndTime}",
            meeting.ParticipantIds, meeting.StartTime, meeting.EndTime);

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully saved meeting {MeetingId} to database", meeting.Id);
        return meeting;
    }

    public async Task<List<Meeting>> GetUserMeetingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving meetings for user {UserId}", userId);

        List<Meeting> meetings = await _context.Meetings
            .Where(m => m.ParticipantIds.Contains(userId))
            .OrderBy(m => m.StartTime)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {MeetingCount} meetings for user {UserId}", meetings.Count, userId);
        return meetings;
    }

    public async Task<List<Meeting>> GetConflictingMeetingsAsync(
        List<int> participantIds,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for conflicting meetings for participants {ParticipantIds} between {StartTime} and {EndTime}",
            participantIds, start, end);

        List<Meeting> meetings = await _context.Meetings
            .Where(m => m.StartTime < end && m.EndTime > start)
            .Where(m => m.ParticipantIds.Any(id => participantIds.Contains(id)))
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {ConflictingMeetingCount} potentially conflicting meetings", meetings.Count);
        return meetings;
    }
}