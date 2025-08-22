using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.DTOs;
using Domain.Meetings;
using Domain.Users;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.MeetingScheduler.Users.CreateUser;

/// <summary>
/// Handles creation of users for the meeting scheduler.
/// </summary>
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IMeetingUserRepository _userRepository;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IMeetingUserRepository userRepository, ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with name: {UserName}", command.Name);

        try
        {
            var user = new MeetingUser(command.Name);

            MeetingUser createdUser = await _userRepository.AddAsync(user, cancellationToken);

            _logger.LogInformation("Successfully created user {UserId} with name: {UserName}",
                createdUser.Id, createdUser.Name);

            var userDto = new UserDto(createdUser.Id, createdUser.Name);

            return Result.Success(userDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "User creation failed for name '{UserName}': {ErrorMessage}",
                command.Name, ex.Message);

            if (ex.Message.Contains("name cannot be empty"))
            {
                return Result.Failure<UserDto>(UserErrors.EmptyName);
            }

            if (ex.Message.Contains("100 characters"))
            {
                return Result.Failure<UserDto>(UserErrors.NameTooLong);
            }

            return Result.Failure<UserDto>(UserErrors.EmptyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating user with name: {UserName}",
                command.Name);
            throw;
        }
    }
}