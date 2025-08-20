using Application.Abstractions.Repositories;
using Application.MeetingScheduler.Services;
using Domain.Meetings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.UnitTests.MeetingScheduler.Services;

public class MeetingSchedulerServiceAlgorithmTests
{
    private readonly Mock<IMeetingRepository> _mockMeetingRepository;
    private readonly MeetingSchedulerService _service;

    public MeetingSchedulerServiceAlgorithmTests()
    {
        _mockMeetingRepository = new Mock<IMeetingRepository>();
        var mockLogger = new Mock<ILogger<MeetingSchedulerService>>();
        _service = new MeetingSchedulerService(_mockMeetingRepository.Object, mockLogger.Object);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNoExistingMeetings_ShouldReturnEarliestSlot()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithEarliestStartBeforeBusinessHours_ShouldReturnBusinessHourStart()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc); // Before 9 AM
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithConflictingMeeting_ShouldFindNextAvailableSlot()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        // Create a conflicting meeting from 10:00-11:00
        var existingMeeting = new Meeting(
            [1, 3], // Participant 1 conflicts
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should find the next available slot after the conflict
        Assert.True(result.Value >= new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNoAvailableSlot_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 30, 0, DateTimeKind.Utc); // 4:30 PM
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);      // 5:00 PM

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result); // Not enough time for 60-minute meeting
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithEmptyParticipantList_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int>();
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithInvalidDuration_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 0; // Invalid duration
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithInvalidTimeRange_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc); // End before start

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithBackToBackMeetings_ShouldFindSlotAfterLastMeeting()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create back-to-back meetings: 10:00-11:00 and 11:00-12:00
        var meeting1 = new Meeting(
            [1, 3],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [1, 4],
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should find slot at 9:00 AM (before conflicts) or after 12:00 PM
        Assert.True(result.Value == new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc) ||
                   result.Value >= new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithPartialOverlap_ShouldAvoidConflict()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 90; // 1.5 hours
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create a meeting that would partially overlap: 11:00-12:30
        var existingMeeting = new Meeting(
            [1, 3],
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should either be at 10:00 (before conflict) or after 12:30
        var expectedEarlySlot = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var expectedLateSlot = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);

        Assert.True(result.Value == expectedEarlySlot || result.Value >= expectedLateSlot);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_AtBusinessHoursBoundary_ShouldRespectBusinessHours()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 30, 0, DateTimeKind.Utc); // 4:30 PM
        var latestEnd = new DateTime(2024, 6, 16, 17, 0, 0, DateTimeKind.Utc); // Next day 5:00 PM

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should skip to next business day at 9:00 AM since 4:30-5:30 PM exceeds business hours
        Assert.Equal(new DateTime(2024, 6, 16, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithMultipleParticipants_ShouldHandleComplexConflicts()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2, 3, 4 }; // 4 participants
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create meetings with different participant combinations
        var meeting1 = new Meeting(
            [1, 5], // Only participant 1 conflicts
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [2, 6], // Only participant 2 conflicts
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var meeting3 = new Meeting(
            [3, 7], // Only participant 3 conflicts
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2, meeting3]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should find slot at 9:00 AM (before all conflicts) or after 1:00 PM
        Assert.True(result.Value == new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc) ||
                   result.Value >= new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNoCommonParticipants_ShouldIgnoreNonConflictingMeetings()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create meetings with no common participants
        var meeting1 = new Meeting(
            [3, 4], // No common participants
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [5, 6], // No common participants
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should return the earliest requested time since no conflicts
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithFullyBookedDay_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create meetings that fill the entire business day
        var meetings = new List<Meeting>();
        for (int hour = 9; hour < 17; hour++)
        {
            meetings.Add(new Meeting(
                [1],
                new DateTime(2024, 6, 15, hour, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 6, 15, hour + 1, 0, 0, DateTimeKind.Utc)));
        }

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result); // No available slots
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithShortMeeting_ShouldFindSmallGaps()
    {
        // Arrange
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 15; // Short meeting
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Create meetings with small gaps
        var meeting1 = new Meeting(
            [1],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 10, 45, 0, DateTimeKind.Utc)); // 45-minute meeting

        var meeting2 = new Meeting(
            [1],
            new DateTime(2024, 6, 15, 11, 15, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc)); // Another meeting

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should find the 30-minute gap between 10:45 and 11:15
        Assert.True(result.Value >= new DateTime(2024, 6, 15, 10, 45, 0, DateTimeKind.Utc) &&
                   result.Value <= new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithTimeRoundingNeeded_ShouldRoundToNext15MinuteSlot()
    {
        // Arrange
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 30;
        var earliestStart = new DateTime(2024, 6, 15, 10, 7, 0, DateTimeKind.Utc); // 10:07 AM
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should round up to 10:15 AM (next 15-minute increment)
        Assert.Equal(new DateTime(2024, 6, 15, 10, 15, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithManyParticipantsAndMeetings_ShouldPerformEfficiently()
    {
        // Arrange - Test algorithm performance with many participants and meetings
        var participantIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }; // 10 participants
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create many meetings with various participant combinations
        var meetings = new List<Meeting>();
        for (int i = 0; i < 20; i++) // 20 existing meetings
        {
            var meetingParticipants = new List<int> { i % 10 + 1, (i + 1) % 10 + 1 };
            var startTime = new DateTime(2024, 6, 15, 9 + i % 8, i % 4 * 15, 0, DateTimeKind.Utc);
            var endTime = startTime.AddMinutes(30);

            if (endTime.Hour < 17) // Keep within business hours
            {
                meetings.Add(new Meeting(meetingParticipants, startTime, endTime));
            }
        }

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        // Act - Measure performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Algorithm should complete within 1 second");
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithLongMeeting_ShouldFindSufficientTimeSlot()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 240; // 4-hour meeting
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Create a short meeting that shouldn't interfere
        var existingMeeting = new Meeting(
            [3, 4], // Different participants
            new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 15, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        // Should find a 4-hour slot from 9:00-13:00
        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithLongMeetingNoSpace_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 480; // 8-hour meeting (longer than business day)
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result); // 8-hour meeting cannot fit in 8-hour business day
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithExactBusinessHoursFit_ShouldReturnSlot()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 480; // Exactly 8 hours (full business day)
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 16, 17, 0, 0, DateTimeKind.Utc); // Next day

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNullParticipantIds_ShouldReturnNull()
    {
        // Arrange
        List<int>? participantIds = null;
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds!, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNegativeDuration_ShouldReturnNull()
    {
        // Arrange
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = -30; // Negative duration
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithSingleParticipant_ShouldWorkCorrectly()
    {
        // Arrange
        var participantIds = new List<int> { 1 }; // Single participant
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var existingMeeting = new Meeting(
            [1],
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        // Act
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }
}