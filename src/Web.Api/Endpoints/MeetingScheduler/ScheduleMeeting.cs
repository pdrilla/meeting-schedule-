using Application.Abstractions.Messaging;
using Application.DTOs;
using Application.MeetingScheduler.Meetings.ScheduleMeeting;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.MeetingScheduler;

internal sealed class ScheduleMeeting : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("meetings", async (
            ScheduleMeetingRequest request,
            ICommandHandler<ScheduleMeetingCommand, MeetingDto> handler,
            ILogger<ScheduleMeeting> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Received meeting scheduling request for {ParticipantCount} participants: {ParticipantIds}, Duration: {Duration} minutes",
                request.ParticipantIds.Count, request.ParticipantIds, request.DurationMinutes);

            var command = new ScheduleMeetingCommand(
                request.ParticipantIds,
                request.DurationMinutes,
                request.EarliestStart,
                request.LatestEnd);

            Result<MeetingDto> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                meeting =>
                {
                    logger.LogInformation("Successfully scheduled meeting {MeetingId} via API at {StartTime} - {EndTime}",
                        meeting.Id, meeting.StartTime, meeting.EndTime);
                    return Results.Created($"/meetings/{meeting.Id}", meeting);
                },
                error =>
                {
                    logger.LogWarning("Failed to schedule meeting via API: {ErrorCode} - {ErrorMessage}. Request: {ParticipantIds}, Duration: {Duration}",
                        error.Error.Code, error.Error.Description, request.ParticipantIds, request.DurationMinutes);
                    return CustomResults.Problem(error);
                });
        })
        .WithTags(Tags.MeetingScheduler)
        .WithSummary("Schedule a new meeting")
        .WithDescription("Schedules a meeting for multiple participants, finding the earliest available time slot")
        .Produces<MeetingDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}