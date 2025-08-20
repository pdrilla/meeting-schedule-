using Application.Abstractions.Messaging;
using Application.DTOs;

namespace Application.MeetingScheduler.Meetings.ScheduleMeeting;

public sealed record ScheduleMeetingCommand(
    List<int> ParticipantIds,
    int DurationMinutes,
    DateTime EarliestStart,
    DateTime LatestEnd) : ICommand<MeetingDto>;