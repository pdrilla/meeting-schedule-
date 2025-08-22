using FluentValidation;

namespace Application.MeetingScheduler.Meetings.ScheduleMeeting;

internal sealed class ScheduleMeetingCommandValidator : AbstractValidator<ScheduleMeetingCommand>
{
    public ScheduleMeetingCommandValidator()
    {
        RuleFor(static x => x.ParticipantIds)
            .NotEmpty()
            .WithMessage("At least one participant is required")
            .WithErrorCode("Meetings.NoParticipants")
            .Must(static ids => ids != null && ids.Count > 0)
            .WithMessage("Participant list cannot be null or empty")
            .WithErrorCode("Meetings.ParticipantListNull")
            .Must(static ids => ids.All(static id => id > 0))
            .WithMessage("All participant IDs must be greater than 0")
            .WithErrorCode("Meetings.InvalidParticipantId")
            .Must(static ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate participants are not allowed")
            .WithErrorCode("Meetings.DuplicateParticipants")
            .Must(static ids => ids.Count <= 50)
            .WithMessage("Cannot have more than 50 participants in a meeting")
            .WithErrorCode("Meetings.TooManyParticipants");

        RuleFor(static x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 minutes")
            .WithErrorCode("Meetings.InvalidDuration")
            .GreaterThanOrEqualTo(15)
            .WithMessage("Minimum meeting duration is 15 minutes")
            .WithErrorCode("Meetings.DurationTooShort")
            .LessThanOrEqualTo(480)
            .WithMessage("Duration cannot exceed 8 hours (480 minutes)")
            .WithErrorCode("Meetings.DurationTooLong")
            .Must(static duration => duration % 15 == 0)
            .WithMessage("Duration must be in 15-minute increments")
            .WithErrorCode("Meetings.InvalidDurationIncrement");

        RuleFor(static x => x.EarliestStart)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Earliest start time must be in the future")
            .WithErrorCode("Meetings.EarliestStartInPast")
            .LessThan(static x => x.LatestEnd)
            .WithMessage("Earliest start time must be before latest end time")
            .WithErrorCode("Meetings.InvalidTimeRange")
            .Must(BeWithinBusinessHours)
            .WithMessage("Earliest start time must be within business hours (09:00-17:00 UTC)")
            .WithErrorCode("Meetings.EarliestStartOutsideBusinessHours");

        RuleFor(static x => x.LatestEnd)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Latest end time must be in the future")
            .WithErrorCode("Meetings.LatestEndInPast")
            .Must(BeWithinBusinessHours)
            .WithMessage("Latest end time must be within business hours (09:00-17:00 UTC)")
            .WithErrorCode("Meetings.LatestEndOutsideBusinessHours");

        RuleFor(static x => x)
            .Must(static x => x.EarliestStart.AddMinutes(x.DurationMinutes) <= x.LatestEnd)
            .WithMessage("The meeting duration must fit within the specified time range")
            .WithErrorCode("Meetings.DurationExceedsTimeRange")
            .Must(static x => (x.LatestEnd - x.EarliestStart).TotalDays <= 30)
            .WithMessage("Time range cannot exceed 30 days")
            .WithErrorCode("Meetings.TimeRangeTooLarge");
    }

    private static bool BeWithinBusinessHours(DateTime dateTime)
    {
        var time = TimeOnly.FromDateTime(dateTime);
        var businessStart = new TimeOnly(9, 0);
        var businessEnd = new TimeOnly(17, 0);

        return time >= businessStart && time <= businessEnd;
    }
}