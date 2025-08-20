using Application.Abstractions.Messaging;
using Application.DTOs;
using Application.MeetingScheduler.Meetings.GetUserMeetings;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.MeetingScheduler;

internal sealed class GetUserMeetings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId}/meetings", async (
            int userId,
            IQueryHandler<GetUserMeetingsQuery, List<MeetingDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserMeetingsQuery(userId);

            Result<List<MeetingDto>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.MeetingScheduler)
        .WithSummary("Get user's meetings")
        .WithDescription("Retrieves all meetings for a specific user, including participant details")
        .Produces<List<MeetingDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}