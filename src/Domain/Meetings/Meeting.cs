using SharedKernel;

namespace Domain.Meetings;

public sealed class Meeting : Entity
{
    private readonly List<int> _participantIds = [];

    public int Id { get; }
    public IReadOnlyList<int> ParticipantIds => _participantIds.AsReadOnly();
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    // Private constructor for EF Core
    private Meeting()
    {
    }

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

    public bool HasConflictWith(Meeting other)
    {
        if (other == null)
        {
            return false;
        }

        // Check if there's any overlap in time
        bool timeOverlap = StartTime < other.EndTime && EndTime > other.StartTime;

        if (!timeOverlap)
        {
            return false;
        }

        // Check if there are common participants
        return _participantIds.Any(id => other._participantIds.Contains(id));
    }

    public bool IsWithinBusinessHours()
    {
        var businessStart = new TimeOnly(9, 0); // 09:00
        var businessEnd = new TimeOnly(17, 0);  // 17:00

        var startTimeOnly = TimeOnly.FromDateTime(StartTime);
        var endTimeOnly = TimeOnly.FromDateTime(EndTime);

        return startTimeOnly >= businessStart && endTimeOnly <= businessEnd;
    }

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
        var businessStart = new TimeOnly(9, 0); // 09:00
        var businessEnd = new TimeOnly(17, 0);  // 17:00

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