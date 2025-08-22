using SharedKernel;

namespace Domain.Meetings;

/// <summary>
/// Represents a scheduled meeting between participants.
/// </summary>
public sealed class Meeting : Entity
{
    private readonly List<int> _participantIds = [];

    /// <summary>
    /// Gets the meeting identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the identifiers of meeting participants.
    /// </summary>
    public IReadOnlyList<int> ParticipantIds => _participantIds.AsReadOnly();

    /// <summary>
    /// Gets the UTC start time of the meeting.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Gets the UTC end time of the meeting.
    /// </summary>
    public DateTime EndTime { get; private set; }

    private Meeting()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Meeting"/> class.
    /// </summary>
    /// <param name="participantIds">Identifiers of meeting participants.</param>
    /// <param name="startTime">Start time in UTC.</param>
    /// <param name="endTime">End time in UTC.</param>
    public Meeting(List<int> participantIds, DateTime startTime, DateTime endTime)
    {
        ValidateTimeSlot(startTime, endTime);
        ValidateBusinessHours(startTime, endTime);
        ValidateParticipants(participantIds);

        _participantIds.AddRange(participantIds);
        StartTime = startTime;
        EndTime = endTime;

        Raise(new MeetingScheduledDomainEvent(Id, participantIds, startTime, endTime));
    }

    /// <summary>
    /// Determines whether this meeting overlaps with another for any participant.
    /// </summary>
    /// <param name="other">The meeting to compare against.</param>
    /// <returns><c>true</c> if a conflict exists; otherwise, <c>false</c>.</returns>
    public bool HasConflictWith(Meeting? other)
    {
        if (other == null)
        {
            return false;
        }

        bool timeOverlap = StartTime < other.EndTime && EndTime > other.StartTime;

        if (!timeOverlap)
        {
            return false;
        }

        return _participantIds.Any(id => other._participantIds.Contains(id));
    }

    /// <summary>
    /// Checks whether the meeting occurs within the configured business hours.
    /// </summary>
    public bool IsWithinBusinessHours()
    {
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        var startTimeOnly = TimeOnly.FromDateTime(StartTime);
        var endTimeOnly = TimeOnly.FromDateTime(EndTime);

        return startTimeOnly >= businessStart && endTimeOnly <= businessEnd;
    }

    /// <summary>
    /// Revalidates the current meeting time slot.
    /// </summary>
    public void ValidateTimeSlot()
    {
        ValidateTimeSlot(StartTime, EndTime);
    }

    private static void ValidateTimeSlot(DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Meeting start time must be before end time");
        }
    }

    private static void ValidateBusinessHours(DateTime startTime, DateTime endTime)
    {
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);

        if (startTimeOnly < businessStart || endTimeOnly > businessEnd)
        {
            throw new ArgumentException("Meeting must be scheduled within business hours (09:00-17:00 UTC)");
        }
    }

    private static void ValidateParticipants(List<int> participantIds)
    {
        if (participantIds == null || participantIds.Count == 0)
        {
            throw new ArgumentException("Meeting must have at least one participant");
        }

        if (participantIds.Distinct().Count() != participantIds.Count)
        {
            throw new ArgumentException("Duplicate participants are not allowed");
        }
    }
}
