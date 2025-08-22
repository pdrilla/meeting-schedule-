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

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithEarliestStartBeforeBusinessHours_ShouldReturnBusinessHourStart()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithConflictingMeeting_ShouldFindNextAvailableSlot()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        var existingMeeting = new Meeting(
            [1, 3],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.True(result.Value >= new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNoAvailableSlot_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 30, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithEmptyParticipantList_ShouldReturnNull()
    {
        var participantIds = new List<int>();
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithInvalidDuration_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 0;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithInvalidTimeRange_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithBackToBackMeetings_ShouldFindSlotAfterLastMeeting()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

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

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.True(result.Value == new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc) ||
                   result.Value >= new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithPartialOverlap_ShouldAvoidConflict()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 90;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

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

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        var expectedEarlySlot = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var expectedLateSlot = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);

        Assert.True(result.Value == expectedEarlySlot || result.Value >= expectedLateSlot);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_AtBusinessHoursBoundary_ShouldRespectBusinessHours()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 16, 30, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 16, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.Equal(new DateTime(2024, 6, 16, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithMultipleParticipants_ShouldHandleComplexConflicts()
    {
        var participantIds = new List<int> { 1, 2, 3, 4 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var meeting1 = new Meeting(
            [1, 5],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [2, 6],
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var meeting3 = new Meeting(
            [3, 7],
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2, meeting3]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.True(result.Value == new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc) ||
                   result.Value >= new DateTime(2024, 6, 15, 13, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNoCommonParticipants_ShouldIgnoreNonConflictingMeetings()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var meeting1 = new Meeting(
            [3, 4],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [5, 6],
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithFullyBookedDay_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

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

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithShortMeeting_ShouldFindSmallGaps()
    {
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 15;
        var earliestStart = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var meeting1 = new Meeting(
            [1],
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 10, 45, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(
            [1],
            new DateTime(2024, 6, 15, 11, 15, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([meeting1, meeting2]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.True(result.Value >= new DateTime(2024, 6, 15, 10, 45, 0, DateTimeKind.Utc) &&
                   result.Value <= new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithTimeRoundingNeeded_ShouldRoundToNext15MinuteSlot()
    {
        var participantIds = new List<int> { 1 };
        const int durationMinutes = 30;
        var earliestStart = new DateTime(2024, 6, 15, 10, 7, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.Equal(new DateTime(2024, 6, 15, 10, 15, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithManyParticipantsAndMeetings_ShouldPerformEfficiently()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var meetings = new List<Meeting>();
        for (int i = 0; i < 20; i++)
        {
            var meetingParticipants = new List<int> { i % 10 + 1, (i + 1) % 10 + 1 };
            var startTime = new DateTime(2024, 6, 15, 9 + i % 8, i % 4 * 15, 0, DateTimeKind.Utc);
            var endTime = startTime.AddMinutes(30);

            if (endTime.Hour < 17)
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

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Algorithm should complete within 1 second");
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithLongMeeting_ShouldFindSufficientTimeSlot()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 240;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var existingMeeting = new Meeting(
            [3, 4],
            new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 15, 0, 0, DateTimeKind.Utc));

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingMeeting]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);

        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithLongMeetingNoSpace_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 480;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithExactBusinessHoursFit_ShouldReturnSlot()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = 480;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 16, 17, 0, 0, DateTimeKind.Utc);

        _mockMeetingRepository
            .Setup(static r => r.GetConflictingMeetingsAsync(
                It.IsAny<List<int>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNullParticipantIds_ShouldReturnNull()
    {
        List<int>? participantIds = null;
        const int durationMinutes = 60;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds!, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithNegativeDuration_ShouldReturnNull()
    {
        var participantIds = new List<int> { 1, 2 };
        const int durationMinutes = -30;
        var earliestStart = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var latestEnd = new DateTime(2024, 6, 15, 17, 0, 0, DateTimeKind.Utc);

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindEarliestAvailableSlot_WithSingleParticipant_ShouldWorkCorrectly()
    {
        var participantIds = new List<int> { 1 };
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

        var result = await _service.FindEarliestAvailableSlotAsync(
            participantIds, durationMinutes, earliestStart, latestEnd);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), result.Value);
    }
}
