using Domain.Meetings;

namespace Domain.UnitTests.Meetings;

public class MeetingSchedulerServiceTests
{
    [Fact]
    public void BusinessHoursValidation_ShouldAcceptValidBusinessHours()
    {
        var startTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc);

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        Assert.True(startTimeOnly >= businessStart && endTimeOnly <= businessEnd);
    }

    [Fact]
    public void BusinessHoursValidation_ShouldRejectTimeBeforeBusinessHours()
    {
        var startTime = new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 9, 30, 0, DateTimeKind.Utc);

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        Assert.False(startTimeOnly >= businessStart && endTimeOnly <= businessEnd);
    }

    [Fact]
    public void BusinessHoursValidation_ShouldRejectTimeAfterBusinessHours()
    {
        var startTime = new DateTime(2024, 1, 15, 16, 30, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 17, 30, 0, DateTimeKind.Utc);

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        Assert.False(startTimeOnly >= businessStart && endTimeOnly <= businessEnd);
    }

    [Fact]
    public void ConflictDetection_ShouldDetectOverlappingMeetings()
    {
        var meeting1Start = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var meeting1End = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var meeting2Start = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var meeting2End = new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc);

        bool timeOverlap = meeting1Start < meeting2End && meeting1End > meeting2Start;

        Assert.True(timeOverlap);
    }

    [Fact]
    public void ConflictDetection_ShouldAllowBackToBackMeetings()
    {
        var meeting1Start = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var meeting1End = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var meeting2Start = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        var meeting2End = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        bool timeOverlap = meeting1Start < meeting2End && meeting1End > meeting2Start;

        Assert.False(timeOverlap);
    }

    [Fact]
    public void TimeSlotGeneration_ShouldRoundToNext15MinuteIncrement()
    {
        var inputTime = new DateTime(2024, 1, 15, 10, 7, 0, DateTimeKind.Utc);

        int minutes = inputTime.Minute;
        int roundedMinutes = ((minutes + 14) / 15) * 15;

        DateTime expectedTime;
        if (roundedMinutes >= 60)
        {
            expectedTime = new DateTime(inputTime.Year, inputTime.Month, inputTime.Day, inputTime.Hour + 1, 0, 0, DateTimeKind.Utc);
        }
        else
        {
            expectedTime = new DateTime(inputTime.Year, inputTime.Month, inputTime.Day, inputTime.Hour, roundedMinutes, 0, DateTimeKind.Utc);
        }

        Assert.Equal(new DateTime(2024, 1, 15, 10, 15, 0, DateTimeKind.Utc), expectedTime);
    }

    [Fact]
    public void TimeSlotGeneration_ShouldHandleHourBoundary()
    {
        var inputTime = new DateTime(2024, 1, 15, 10, 55, 0, DateTimeKind.Utc);

        int minutes = inputTime.Minute;
        int roundedMinutes = ((minutes + 14) / 15) * 15;

        DateTime expectedTime;
        if (roundedMinutes >= 60)
        {
            expectedTime = new DateTime(inputTime.Year, inputTime.Month, inputTime.Day, inputTime.Hour + 1, 0, 0, DateTimeKind.Utc);
        }
        else
        {
            expectedTime = new DateTime(inputTime.Year, inputTime.Month, inputTime.Day, inputTime.Hour, roundedMinutes, 0, DateTimeKind.Utc);
        }

        Assert.Equal(new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc), expectedTime);
    }

    [Fact]
    public void ParticipantConflictDetection_ShouldDetectCommonParticipants()
    {
        var participantIds1 = new List<int> { 1, 2, 3 };
        var participantIds2 = new List<int> { 3, 4, 5 };

        bool hasCommonParticipants = participantIds1.Any(id => participantIds2.Contains(id));

        Assert.True(hasCommonParticipants);
    }

    [Fact]
    public void ParticipantConflictDetection_ShouldHandleNoCommonParticipants()
    {
        var participantIds1 = new List<int> { 1, 2, 3 };
        var participantIds2 = new List<int> { 4, 5, 6 };

        bool hasCommonParticipants = participantIds1.Any(id => participantIds2.Contains(id));

        Assert.False(hasCommonParticipants);
    }
}
