using FluentValidation;

namespace Application.MeetingScheduler.Users.CreateUser;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("User name is required")
            .WithErrorCode("Users.NameRequired")
            .MaximumLength(100)
            .WithMessage("User name cannot exceed 100 characters")
            .WithErrorCode("Users.NameTooLong")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("User name can only contain letters, spaces, hyphens, apostrophes, and periods")
            .WithErrorCode("Users.NameInvalidCharacters");
    }
}