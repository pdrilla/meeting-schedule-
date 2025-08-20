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

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2, 3 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            new("User 1"),
            new("User 2"),
            new("User 3")
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

        // Act
        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Participants.Count);
        Assert.Equal(availableSlot, result.Value.StartTime);
        Assert.Equal(availableSlot.AddMinutes(durationMinutes), result.Value.EndTime);
    }

    [Fact]
    public async Task Handle_WithMissingParticipants_ShouldReturnFailureResult()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2, 3 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        // Only return 2 participants instead of 3
        var participants = new List<MeetingUser>
        {
            new("User 1"),
            new("User 2")
        };

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        // Act
        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound(3), result.Error);
    }

    [Fact]
    public async Task Handle_WithNoAvailableSlot_ShouldReturnFailureResult()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            new("User 1"),
            new("User 2")
        };

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(participantIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _mockMeetingSchedulerService
            .Setup(s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd))
            .ReturnsAsync((DateTime?)null);

        // Act
        Result<MeetingDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(MeetingErrors.NoAvailableSlot, result.Error);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallMeetingSchedulerService()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 30;
        var earliestStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var command = new ScheduleMeetingCommand(participantIds, durationMinutes, earliestStart, latestEnd);

        var participants = new List<MeetingUser>
        {
            new("User 1"),
            new("User 2")
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

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMeetingSchedulerService.Verify(
            s => s.FindEarliestAvailableSlotAsync(participantIds, durationMinutes, earliestStart, latestEnd),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallMeetingRepository()
    {
        // Arrange
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

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMeetingRepository.Verify(
            r => r.AddAsync(It.Is<Meeting>(m =>
                m.StartTime == availableSlot &&
                m.EndTime == availableSlot.AddMinutes(durationMinutes)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}