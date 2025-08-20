using FluentValidation;

namespace Application.MeetingScheduler.Meetings.GetUserMeetings;

internal sealed class GetUserMeetingsQueryValidator : AbstractValidator<GetUserMeetingsQuery>
{
    public GetUserMeetingsQueryValidator()
    {
        RuleFor(static x => x.UserId)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0")
            .WithErrorCode("Users.InvalidUserId")
            .LessThan(int.MaxValue)
            .WithMessage("User ID is too large")
            .WithErrorCode("Users.UserIdTooLarge");
    }
}