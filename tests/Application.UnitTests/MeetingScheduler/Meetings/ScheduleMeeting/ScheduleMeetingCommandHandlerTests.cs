using System.Reflection;
using Application.Abstractions.Repositories;
using Application.DTOs;
using Application.MeetingScheduler.Meetings.ScheduleMeeting;
using Domain.Meetings;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel;

namespace Application.UnitTests.MeetingScheduler.Meetings.ScheduleMeeting;

public class ScheduleMeetingCommandHandlerTests
{
    private readonly Mock<IMeetingSchedulerService> _mockMeetingSchedulerService;
    private readonly Mock<IMeetingRepository> _mockMeetingRepository;
    private readonly Mock<IMeetingUserRepository> _mockUserRepository;
    private readonly ScheduleMeetingCommandHandler _handler;

    public ScheduleMeetingCommandHandlerTests()
    {
        _mockMeetingSchedulerService = new Mock<IMeetingSchedulerService>();
        _mockMeetingRepository = new Mock<IMeetingRepository>();
        _mockUserRepository = new Mock<IMeetingUserRepository>();
        var mockLogger = new Mock<ILogger<ScheduleMeetingCommandHandler>>();

        _handler = new ScheduleMeetingCommandHandler(
            _mockMeetingSchedulerService.Object,
            _mockMeetingRepository.Object,
            _mockUserRepository.Object,
            mockLogger.Object);
    }

    private static MeetingUser CreateUser(int id, string name)
    {
        var user = new MeetingUser(name);
        typeof(MeetingUser).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(user, id);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        var participantIds = new List<int> { 1, 2, 3 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            CreateUser(1, "User 1"),
            CreateUser(2, "User 2"),
            CreateUser(3, "User 3")
        };

        var availableSlot = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var meeting = new Meeting(participantIds, availableSlot, availableSlot.AddMinutes(durationMinutes));

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _mockMeetingSchedulerService
            .Setup(s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd))
            .ReturnsAsync(availableSlot);

        _mockMeetingRepository
            .Setup(r => r.AddAsync(It.IsAny<Meeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);

        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Participants.Count);
        Assert.Equal(availableSlot, result.Value.StartTime);
        Assert.Equal(availableSlot.AddMinutes(durationMinutes), result.Value.EndTime);
    }

    [Fact]
    public async Task Handle_WithMissingParticipants_ShouldReturnFailureResult()
    {
        var participantIds = new List<int> { 1, 2, 3 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            CreateUser(1, "User 1"),
            CreateUser(2, "User 2")
        };

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound(3), result.Error);
    }

    [Fact]
    public async Task Handle_WithNoAvailableSlot_ShouldReturnFailureResult()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            CreateUser(1, "User 1"),
            CreateUser(2, "User 2")
        };

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _mockMeetingSchedulerService
            .Setup(s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd))
            .ReturnsAsync((DateTime?)null);

        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MeetingErrors.NoAvailableSlot, result.Error);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallMeetingSchedulerService()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 30;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            CreateUser(1, "User 1"),
            CreateUser(2, "User 2")
        };

        var availableSlot = new DateTime(2024, 1, 15, 14, 0, 0, DateTimeKind.Utc);
        var meeting = new Meeting(participantIds, availableSlot, availableSlot.AddMinutes(durationMinutes));

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _mockMeetingSchedulerService
            .Setup(s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd))
            .ReturnsAsync(availableSlot);

        _mockMeetingRepository
            .Setup(r => r.AddAsync(It.IsAny<Meeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);

        await _handler.Handle(command, CancellationToken.None);

        _mockMeetingSchedulerService.Verify(
            s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallMeetingRepository()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 45;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            new("User 1"),
            new("User 2")
        };

        var availableSlot = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        var meeting = new Meeting(participantIds, availableSlot, availableSlot.AddMinutes(durationMinutes));

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _mockMeetingSchedulerService
            .Setup(s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd))
            .ReturnsAsync(availableSlot);

        _mockMeetingRepository
            .Setup(r => r.AddAsync(It.IsAny<Meeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);

        await _handler.Handle(command, CancellationToken.None);

        _mockMeetingRepository.Verify(
            r => r.AddAsync(It.Is<Meeting>(m =>
                m.StartTime == availableSlot &&
                m.EndTime == availableSlot.AddMinutes(durationMinutes)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
