using Application.Abstractions.Messaging;
using Application.DTOs;

namespace Application.MeetingScheduler.Meetings.GetUserMeetings;

public sealed record GetUserMeetingsQuery(int UserId) : IQuery<List<MeetingDto>>;