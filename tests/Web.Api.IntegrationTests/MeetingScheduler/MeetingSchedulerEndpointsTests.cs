using Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Web.Api;

namespace Web.Api.IntegrationTests.MeetingScheduler;

public class MeetingSchedulerEndpointsTests
{
    [Fact]
    public async Task CreateUser_WithValidRequest_ShouldReturnCreatedUser()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/users", new { Name = "Alice" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        UserDto? user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("Alice", user!.Name);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task ScheduleMeeting_WithValidRequest_ShouldReturnCreatedMeeting()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        UserDto user1 = await CreateUserAsync(client, "Alice");
        UserDto user2 = await CreateUserAsync(client, "Bob");

        DateTime earliestStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
        DateTime latestEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(17);

        var request = new
        {
            ParticipantIds = new[] { user1.Id, user2.Id },
            DurationMinutes = 60,
            EarliestStart = earliestStart,
            LatestEnd = latestEnd
        };

        var response = await client.PostAsJsonAsync("/meetings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        MeetingDto? meeting = await response.Content.ReadFromJsonAsync<MeetingDto>();
        Assert.NotNull(meeting);
        Assert.Equal(2, meeting!.Participants.Count);
    }

    [Fact]
    public async Task ScheduleMeeting_WithMissingUser_ShouldReturnNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        DateTime earliestStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
        DateTime latestEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(17);

        var request = new
        {
            ParticipantIds = new[] { 999 },
            DurationMinutes = 60,
            EarliestStart = earliestStart,
            LatestEnd = latestEnd
        };

        var response = await client.PostAsJsonAsync("/meetings", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserMeetings_WhenMeetingExists_ShouldReturnMeetings()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        UserDto user1 = await CreateUserAsync(client, "Alice");
        UserDto user2 = await CreateUserAsync(client, "Bob");

        DateTime earliestStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
        DateTime latestEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(17);

        var scheduleRequest = new
        {
            ParticipantIds = new[] { user1.Id, user2.Id },
            DurationMinutes = 30,
            EarliestStart = earliestStart,
            LatestEnd = latestEnd
        };

        var scheduleResponse = await client.PostAsJsonAsync("/meetings", scheduleRequest);
        scheduleResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/users/{user1.Id}/meetings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<MeetingDto>? meetings = await response.Content.ReadFromJsonAsync<List<MeetingDto>>();
        Assert.NotNull(meetings);
        Assert.Single(meetings!);
        Assert.Contains(meetings, m => m.Participants.Any(p => p.Id == user1.Id));
    }

    private static async Task<UserDto> CreateUserAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/users", new { Name = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }
}
