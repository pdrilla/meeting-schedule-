using Application.Abstractions.Messaging;
using Application.DTOs;
using Application.MeetingScheduler.Users.CreateUser;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.MeetingScheduler;

internal sealed class CreateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users", async (
            CreateUserRequest request,
            ICommandHandler<CreateUserCommand, UserDto> handler,
            ILogger<CreateUser> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Received request to create user with name: {UserName}", request.Name);

            var command = new CreateUserCommand(request.Name);

            Result<UserDto> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                user =>
                {
                    logger.LogInformation("Successfully created user {UserId} via API", user.Id);
                    return Results.Created($"/users/{user.Id}", user);
                },
                error =>
                {
                    logger.LogWarning("Failed to create user via API: {ErrorCode} - {ErrorMessage}",
                        error.Error.Code, error.Error.Description);
                    return CustomResults.Problem(error);
                });
        })
        .WithTags(Tags.MeetingScheduler)
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user in the meeting scheduler system")
        .Produces<UserDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}