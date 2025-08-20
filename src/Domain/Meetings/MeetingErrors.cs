using SharedKernel;

namespace Domain.Meetings;

public static class MeetingErrors
{
    public static readonly Error InvalidTimeSlot = Error.Failure(
        "Meeting.InvalidTimeSlot",
        "Meeting start time must be before end time");

    public static readonly Error OutsideBusinessHours = Error.Failure(
        "Meeting.OutsideBusinessHours",
        "Meeting must be scheduled within business hours (09:00-17:00 UTC)");

    public static readonly Error NoParticipants = Error.Failure(
        "Meeting.NoParticipants",
        "Meeting must have at least one participant");

    public static readonly Error DuplicateParticipants = Error.Failure(
        "Meeting.DuplicateParticipants",
        "Duplicate participants are not allowed");

    public static readonly Error ConflictDetected = Error.Conflict(
        "Meeting.ConflictDetected",
        "Meeting conflicts with existing meetings for one or more participants");

    public static readonly Error NoAvailableSlot = Error.NotFound(
        "Meeting.NoAvailableSlot",
        "No available time slot found within the specified constraints");

    public static readonly Error NotFound = Error.NotFound(
        "Meeting.NotFound",
        "Meeting not found");
}