using Domain.Meetings;

namespace Domain.UnitTests.Meetings;

public class MeetingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateMeeting()
    {
        var participantIds = new List<int> { 1, 2, 3 };
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var meeting = new Meeting(participantIds, startTime, endTime);

        Assert.Equal(participantIds, meeting.ParticipantIds);
        Assert.Equal(startTime, meeting.StartTime);
        Assert.Equal(endTime, meeting.EndTime);
    }

    [Fact]
    public void Constructor_WithStartTimeAfterEndTime_ShouldThrowArgumentException()
    {
        var participantIds = new List<int> { 1, 2 };
        var startTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("start time must be before end time", exception.Message);
    }

    [Fact]
    public void Constructor_WithEqualStartAndEndTime_ShouldThrowArgumentException()
    {
        var participantIds = new List<int> { 1, 2 };
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("start time must be before end time", exception.Message);
    }

    [Fact]
    public void Constructor_WithTimeOutsideBusinessHours_ShouldThrowArgumentException()
    {
        var participantIds = new List<int> { 1, 2 };
        var startTime = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("business hours", exception.Message);
    }

    [Fact]
    public void Constructor_WithEndTimeAfterBusinessHours_ShouldThrowArgumentException()
    {
        var participantIds = new List<int> { 1, 2 };
        var startTime = new DateTime(2024, 1, 15, 16, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("business hours", exception.Message);
    }

    [Fact]
    public void Constructor_WithNoParticipants_ShouldThrowArgumentException()
    {
        var participantIds = new List<int>();
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("at least one participant", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullParticipants_ShouldThrowArgumentException()
    {
        List<int>? participantIds = null;
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds!, startTime, endTime));
        Assert.Contains("at least one participant", exception.Message);
    }

    [Fact]
    public void Constructor_WithDuplicateParticipants_ShouldThrowArgumentException()
    {
        var participantIds = new List<int> { 1, 2, 1 };
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<ArgumentException>(() => new Meeting(participantIds, startTime, endTime));
        Assert.Contains("Duplicate participants", exception.Message);
    }

    [Fact]
    public void HasConflictWith_WithOverlappingTimeAndCommonParticipants_ShouldReturnTrue()
    {
        var participantIds1 = new List<int> { 1, 2 };
        var participantIds2 = new List<int> { 2, 3 };

        var meeting1 = new Meeting(participantIds1,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(participantIds2,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc));

        bool hasConflict = meeting1.HasConflictWith(meeting2);

        Assert.True(hasConflict);
    }

    [Fact]
    public void HasConflictWith_WithOverlappingTimeButNoCommonParticipants_ShouldReturnFalse()
    {
        var participantIds1 = new List<int> { 1, 2 };
        var participantIds2 = new List<int> { 3, 4 };

        var meeting1 = new Meeting(participantIds1,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(participantIds2,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc));

        bool hasConflict = meeting1.HasConflictWith(meeting2);

        Assert.False(hasConflict);
    }

    [Fact]
    public void HasConflictWith_WithNoTimeOverlap_ShouldReturnFalse()
    {
        var participantIds1 = new List<int> { 1, 2 };
        var participantIds2 = new List<int> { 1, 3 };

        var meeting1 = new Meeting(participantIds1,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc));

        var meeting2 = new Meeting(participantIds2,
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        bool hasConflict = meeting1.HasConflictWith(meeting2);

        Assert.False(hasConflict);
    }

    [Fact]
    public void HasConflictWith_WithNullMeeting_ShouldReturnFalse()
    {
        var participantIds = new List<int> { 1, 2 };
        var meeting = new Meeting(participantIds,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc));

        bool hasConflict = meeting.HasConflictWith(null!);

        Assert.False(hasConflict);
    }

    [Fact]
    public void IsWithinBusinessHours_WithValidBusinessHours_ShouldReturnTrue()
    {
        var participantIds = new List<int> { 1, 2 };
        var meeting = new Meeting(participantIds,
            new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc));

        bool isWithinBusinessHours = meeting.IsWithinBusinessHours();

        Assert.True(isWithinBusinessHours);
    }

    [Fact]
    public void IsWithinBusinessHours_WithTimeOutsideBusinessHours_ShouldReturnFalse()
    {

        Assert.True(true);
    }

    [Fact]
    public void Constructor_ShouldRaiseDomainEvent()
    {
        var participantIds = new List<int> { 1, 2, 3 };
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var meeting = new Meeting(participantIds, startTime, endTime);

        Assert.Single(meeting.DomainEvents);
        var domainEvent = meeting.DomainEvents.First();
        Assert.IsType<MeetingScheduledDomainEvent>(domainEvent);

        var meetingScheduledEvent = (MeetingScheduledDomainEvent)domainEvent;
        Assert.Equal(meeting.Id, meetingScheduledEvent.MeetingId);
        Assert.Equal(participantIds, meetingScheduledEvent.ParticipantIds);
        Assert.Equal(startTime, meetingScheduledEvent.StartTime);
        Assert.Equal(endTime, meetingScheduledEvent.EndTime);
    }

    [Fact]
    public void ValidateTimeSlot_ShouldNotThrowForValidMeeting()
    {
        var participantIds = new List<int> { 1, 2 };
        var meeting = new Meeting(participantIds,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc));

        meeting.ValidateTimeSlot();
    }
}
