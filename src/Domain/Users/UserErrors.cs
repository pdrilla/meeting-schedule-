using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(int userId)
    {
        return Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");
    }

    public static readonly Error UserNotFound = Error.NotFound(
        "Users.NotFound",
        "User not found");

    public static readonly Error EmptyName = Error.Failure(
        "Users.EmptyName",
        "User name cannot be empty");

    public static readonly Error NameTooLong = Error.Failure(
        "Users.NameTooLong",
        "User name cannot exceed 100 characters");
}
