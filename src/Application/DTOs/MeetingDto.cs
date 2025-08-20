namespace Application.DTOs;

public sealed record MeetingDto(
    int Id,
    List<UserDto> Participants,
    DateTime StartTime,
    DateTime EndTime);