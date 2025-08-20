using Application.Abstractions.Repositories;
using Domain.Meetings;
using Microsoft.Extensions.Logging;

namespace Application.MeetingScheduler.Services;

internal sealed class MeetingSchedulerService : IMeetingSchedulerService
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ILogger<MeetingSchedulerService> _logger;

    public MeetingSchedulerService(IMeetingRepository meetingRepository, ILogger<MeetingSchedulerService> logger)
    {
        _meetingRepository = meetingRepository;
        _logger = logger;
    }

    public async Task<DateTime?> FindEarliestAvailableSlotAsync(
        List<int> participantIds,
        int durationMinutes,
        DateTime earliestStart,
        DateTime latestEnd)
    {
        _logger.LogDebug("Starting scheduling algorithm for {ParticipantCount} participants, duration {Duration} minutes",
            participantIds?.Count ?? 0, durationMinutes);

        // Validate input parameters
        if (participantIds == null || participantIds.Count == 0)
        {
            _logger.LogWarning("Scheduling failed: No participants provided");
            return null;
        }

        if (durationMinutes <= 0)
        {
            _logger.LogWarning("Scheduling failed: Invalid duration {Duration}", durationMinutes);
            return null;
        }

        if (earliestStart >= latestEnd)
        {
            _logger.LogWarning("Scheduling failed: Invalid time range {EarliestStart} >= {LatestEnd}",
                earliestStart, latestEnd);
            return null;
        }

        // Get all existing meetings for the participants within the time range
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        List<Meeting> existingMeetings = await _meetingRepository.GetConflictingMeetingsAsync(
            participantIds,
            earliestStart.Date,
            latestEnd.Date.AddDays(1));
        stopwatch.Stop();

        _logger.LogDebug("Retrieved {MeetingCount} existing meetings in {ElapsedMs}ms",
            existingMeetings.Count, stopwatch.ElapsedMilliseconds);

        // Generate potential time slots and find the first available one
        DateTime currentSlot = GetNextBusinessHourSlot(earliestStart);
        int slotsChecked = 0;

        while (currentSlot.AddMinutes(durationMinutes) <= latestEnd)
        {
            DateTime slotEnd = currentSlot.AddMinutes(durationMinutes);
            slotsChecked++;

            // Check if this slot is within business hours and has no conflicts
            if (IsWithinBusinessHours(currentSlot, slotEnd) &&
                !HasConflictWithExistingMeetings(currentSlot, slotEnd, existingMeetings, participantIds))
            {
                _logger.LogInformation("Found available slot after checking {SlotsChecked} slots: {StartTime} - {EndTime}",
                    slotsChecked, currentSlot, slotEnd);
                return currentSlot;
            }

            // Move to next potential slot (15-minute increments for efficiency)
            currentSlot = currentSlot.AddMinutes(15);

            // Skip to next business day if we've passed business hours
            if (!IsWithinBusinessHours(currentSlot, currentSlot))
            {
                currentSlot = GetNextBusinessHourSlot(currentSlot.Date.AddDays(1));
            }
        }

        _logger.LogWarning("No available slot found after checking {SlotsChecked} slots for {ParticipantCount} participants",
            slotsChecked, participantIds.Count);
        return null; // No available slot found
    }

    private static DateTime GetNextBusinessHourSlot(DateTime dateTime)
    {
        var businessStart = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 9, 0, 0, DateTimeKind.Utc);

        if (dateTime <= businessStart)
        {
            return businessStart;
        }

        // If it's already past business hours, move to next day
        var businessEnd = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 17, 0, 0, DateTimeKind.Utc);
        if (dateTime >= businessEnd)
        {
            return GetNextBusinessHourSlot(dateTime.Date.AddDays(1));
        }

        // Round up to next 15-minute increment within business hours
        int minutes = dateTime.Minute;
        int roundedMinutes = (minutes + 14) / 15 * 15;

        if (roundedMinutes >= 60)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour + 1, 0, 0, DateTimeKind.Utc);
        }

        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, roundedMinutes, 0, DateTimeKind.Utc);
    }

    private static bool IsWithinBusinessHours(DateTime startTime, DateTime endTime)
    {
        var businessStart = new TimeOnly(9, 0); // 09:00
        var businessEnd = new TimeOnly(17, 0);  // 17:00

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);

        return startTimeOnly >= businessStart && endTimeOnly <= businessEnd;
    }

    private static bool HasConflictWithExistingMeetings(
        DateTime slotStart,
        DateTime slotEnd,
        List<Meeting> existingMeetings,
        List<int> participantIds)
    {
        foreach (Meeting meeting in existingMeetings)
        {
            // Check if there's any time overlap
            bool timeOverlap = slotStart < meeting.EndTime && slotEnd > meeting.StartTime;

            if (timeOverlap)
            {
                // Check if there are common participants
                bool hasCommonParticipants = participantIds.Any(id => meeting.ParticipantIds.Contains(id));

                if (hasCommonParticipants)
                {
                    return true; // Conflict found
                }
            }
        }

        return false; // No conflicts
    }
}