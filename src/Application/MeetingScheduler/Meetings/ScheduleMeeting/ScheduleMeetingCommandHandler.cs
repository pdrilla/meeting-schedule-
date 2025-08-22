using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.DTOs;
using Domain.Meetings;
using Domain.Users;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.MeetingScheduler.Meetings.ScheduleMeeting;

/// <summary>
/// Schedules meetings while avoiding participant conflicts.
/// </summary>
public sealed class ScheduleMeetingCommandHandler : ICommandHandler<ScheduleMeetingCommand, MeetingDto>
{
    private readonly IMeetingSchedulerService _meetingSchedulerService;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingUserRepository _userRepository;
    private readonly ILogger<ScheduleMeetingCommandHandler> _logger;

    public ScheduleMeetingCommandHandler(
        IMeetingSchedulerService meetingSchedulerService,
        IMeetingRepository meetingRepository,
        IMeetingUserRepository userRepository,
        ILogger<ScheduleMeetingCommandHandler> logger)
    {
        _meetingSchedulerService = meetingSchedulerService;
        _meetingRepository = meetingRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<MeetingDto>> Handle(ScheduleMeetingCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling meeting for {ParticipantCount} participants: {ParticipantIds}, Duration: {Duration} minutes, Time window: {EarliestStart} - {LatestEnd}",
            command.ParticipantIds.Count, command.ParticipantIds, command.DurationMinutes, command.EarliestStart, command.LatestEnd);

        List<MeetingUser> participants = await _userRepository.GetByIdsAsync(command.ParticipantIds, cancellationToken);

        if (participants.Count != command.ParticipantIds.Count)
        {
            var foundIds = participants.Select(static p => p.Id).ToList();
            var missingIds = command.ParticipantIds.Except(foundIds).ToList();

            _logger.LogWarning("Meeting scheduling failed: Missing participants {MissingIds}", missingIds);
            return Result.Failure<MeetingDto>(UserErrors.NotFound(missingIds[0]));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        DateTime? availableSlot = await _meetingSchedulerService.FindEarliestAvailableSlotAsync(
            command.ParticipantIds,
            command.DurationMinutes,
            command.EarliestStart,
            command.LatestEnd);
        stopwatch.Stop();

        _logger.LogInformation("Scheduling algorithm completed in {ElapsedMs}ms for {ParticipantCount} participants",
            stopwatch.ElapsedMilliseconds, command.ParticipantIds.Count);

        if (availableSlot == null)
        {
            _logger.LogWarning("No available time slot found for meeting with participants {ParticipantIds} in window {EarliestStart} - {LatestEnd}",
                command.ParticipantIds, command.EarliestStart, command.LatestEnd);
            return Result.Failure<MeetingDto>(MeetingErrors.NoAvailableSlot);
        }

        DateTime startTime = availableSlot.Value;
        DateTime endTime = startTime.AddMinutes(command.DurationMinutes);

        _logger.LogInformation("Found available slot: {StartTime} - {EndTime}", startTime, endTime);

        try
        {
            var meeting = new Meeting(command.ParticipantIds, startTime, endTime);
            Meeting createdMeeting = await _meetingRepository.AddAsync(meeting, cancellationToken);

            _logger.LogInformation("Successfully created meeting {MeetingId} for participants {ParticipantIds} at {StartTime} - {EndTime}",
                createdMeeting.Id, command.ParticipantIds, startTime, endTime);

            var participantDtos = participants
                .Select(static p => new UserDto(p.Id, p.Name))
                .ToList();

            var meetingDto = new MeetingDto(
                createdMeeting.Id,
                participantDtos,
                createdMeeting.StartTime,
                createdMeeting.EndTime);

            return Result.Success(meetingDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meeting creation failed for participants {ParticipantIds}: {ErrorMessage}",
                command.ParticipantIds, ex.Message);

            if (ex.Message.Contains("business hours"))
            {
                return Result.Failure<MeetingDto>(MeetingErrors.OutsideBusinessHours);
            }

            if (ex.Message.Contains("start time"))
            {
                return Result.Failure<MeetingDto>(MeetingErrors.InvalidTimeSlot);
            }

            if (ex.Message.Contains("participant"))
            {
                return Result.Failure<MeetingDto>(MeetingErrors.NoParticipants);
            }

            return Result.Failure<MeetingDto>(MeetingErrors.InvalidTimeSlot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while scheduling meeting for participants {ParticipantIds}",
                command.ParticipantIds);
            throw;
        }
    }
}