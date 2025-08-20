using Application.Abstractions.Messaging;
using Application.DTOs;

namespace Application.MeetingScheduler.Users.CreateUser;

public sealed record CreateUserCommand(string Name) : ICommand<UserDto>;