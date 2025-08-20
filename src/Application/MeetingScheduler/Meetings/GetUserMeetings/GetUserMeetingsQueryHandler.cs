using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.DTOs;
using Domain.Meetings;
using Domain.Users;
using SharedKernel;

namespace Application.MeetingScheduler.Meetings.GetUserMeetings;

internal sealed class GetUserMeetingsQueryHandler : IQueryHandler<GetUserMeetingsQuery, List<MeetingDto>>
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingUserRepository _userRepository;

    public GetUserMeetingsQueryHandler(
        IMeetingRepository meetingRepository,
        IMeetingUserRepository userRepository)
    {
        _meetingRepository = meetingRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<List<MeetingDto>>> Handle(GetUserMeetingsQuery query, CancellationToken cancellationToken)
    {
        // Verify that the user exists
        MeetingUser? user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<List<MeetingDto>>(UserErrors.NotFound(query.UserId));
        }

        // Get all meetings for the user
        List<Meeting> meetings = await _meetingRepository.GetUserMeetingsAsync(query.UserId, cancellationToken);

        // Handle empty case
        if (meetings.Count == 0)
        {
            return Result.Success(new List<MeetingDto>());
        }

        // Get all unique participant IDs to minimize database calls
        HashSet<int> allParticipantIds = [];
        foreach (Meeting meeting in meetings)
        {
            foreach (int participantId in meeting.ParticipantIds)
            {
                allParticipantIds.Add(participantId);
            }
        }

        // Get all participants in one database call
        List<MeetingUser> allParticipants = await _userRepository.GetByIdsAsync(
            [.. allParticipantIds],
            cancellationToken);

        // Create a lookup dictionary for fast participant access
        var participantLookup = allParticipants.ToDictionary(p => p.Id);

        // Convert to DTOs
        List<MeetingDto> meetingDtos = [];

        foreach (Meeting meeting in meetings)
        {
            var participantDtos = meeting.ParticipantIds
                .Where(participantLookup.ContainsKey)
                .Select(id => participantLookup[id])
                .Select(p => new UserDto(p.Id, p.Name))
                .ToList();

            var meetingDto = new MeetingDto(
                meeting.Id,
                participantDtos,
                meeting.StartTime,
                meeting.EndTime);

            meetingDtos.Add(meetingDto);
        }

        return Result.Success(meetingDtos);
    }
}