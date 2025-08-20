using Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Web.Api.IntegrationTests.MeetingScheduler;

public class GetUserMeetingsEndpointTests : BaseIntegrationTest
{
    public GetUserMeetingsEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserMeetings_WithValidUserId_ShouldReturnUserMeetings()
    {
        // Arrange
        // Create users
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Two"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // Schedule a meeting for user1
        var meetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        var meetingResponse = await HttpClient.PostAsJsonAsync("/meetings", meetingRequest);
        Assert.Equal(HttpStatusCode.Created, meetingResponse.StatusCode);

        // Act
        var response = await HttpClient.GetAsync($"/users/{user1.Id}/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meetings = await DeserializeResponse<List<MeetingDto>>(response);
        Assert.NotNull(meetings);
        Assert.Single(meetings);

        var meeting = meetings[0];
        Assert.Equal(2, meeting.Participants.Count);
        Assert.Contains(meeting.Participants, p => p.Name == "User One");
        Assert.Contains(meeting.Participants, p => p.Name == "User Two");
    }

    [Fact]
    public async Task GetUserMeetings_WithNonExistentUserId_ShouldReturnNotFound()
    {
        // Act
        var response = await HttpClient.GetAsync("/users/999/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserMeetings_WithUserHavingNoMeetings_ShouldReturnEmptyList()
    {
        // Arrange
        var userResponse = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("Lonely User"));
        var user = await DeserializeResponse<UserDto>(userResponse);
        Assert.NotNull(user);

        // Act
        var response = await HttpClient.GetAsync($"/users/{user.Id}/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meetings = await DeserializeResponse<List<MeetingDto>>(response);
        Assert.NotNull(meetings);
        Assert.Empty(meetings);
    }

    [Fact]
    public async Task GetUserMeetings_WithMultipleMeetings_ShouldReturnAllMeetings()
    {
        // Arrange
        // Create users
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Two"));
        var user3Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Three"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);
        var user3 = await DeserializeResponse<UserDto>(user3Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotNull(user3);

        // Schedule first meeting
        var meeting1Request = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc)
        );

        var meeting1Response = await HttpClient.PostAsJsonAsync("/meetings", meeting1Request);
        Assert.Equal(HttpStatusCode.Created, meeting1Response.StatusCode);

        // Schedule second meeting
        var meeting2Request = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user3.Id },
            60,
            new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        var meeting2Response = await HttpClient.PostAsJsonAsync("/meetings", meeting2Request);
        Assert.Equal(HttpStatusCode.Created, meeting2Response.StatusCode);

        // Act
        var response = await HttpClient.GetAsync($"/users/{user1.Id}/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meetings = await DeserializeResponse<List<MeetingDto>>(response);
        Assert.NotNull(meetings);
        Assert.Equal(2, meetings.Count);

        // Verify both meetings are returned
        Assert.All(meetings, meeting =>
            Assert.Contains(meeting.Participants, p => p.Name == "User One"));
    }

    [Fact]
    public async Task GetUserMeetings_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync("/users/0/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserMeetings_WithNegativeUserId_ShouldReturnBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync("/users/-1/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserMeetings_ShouldReturnMeetingsInChronologicalOrder()
    {
        // Arrange
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Two"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // Schedule meetings in different order
        var laterMeetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60,
            new DateTime(2024, 6, 15, 15, 0, 0, DateTimeKind.Utc), // Later meeting
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        var earlierMeetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), // Earlier meeting
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc)
        );

        // Schedule later meeting first
        var laterResponse = await HttpClient.PostAsJsonAsync("/meetings", laterMeetingRequest);
        Assert.Equal(HttpStatusCode.Created, laterResponse.StatusCode);

        // Schedule earlier meeting second
        var earlierResponse = await HttpClient.PostAsJsonAsync("/meetings", earlierMeetingRequest);
        Assert.Equal(HttpStatusCode.Created, earlierResponse.StatusCode);

        // Act
        var response = await HttpClient.GetAsync($"/users/{user1.Id}/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meetings = await DeserializeResponse<List<MeetingDto>>(response);
        Assert.NotNull(meetings);
        Assert.Equal(2, meetings.Count);

        // Verify meetings are returned in chronological order
        Assert.True(meetings[0].StartTime <= meetings[1].StartTime);
    }

    [Fact]
    public async Task GetUserMeetings_ShouldIncludeAllParticipantDetails()
    {
        // Arrange
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("Alice"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("Bob"));
        var user3Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("Charlie"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);
        var user3 = await DeserializeResponse<UserDto>(user3Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotNull(user3);

        // Schedule meeting with all three users
        var meetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id, user3.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        var meetingResponse = await HttpClient.PostAsJsonAsync("/meetings", meetingRequest);
        Assert.Equal(HttpStatusCode.Created, meetingResponse.StatusCode);

        // Act
        var response = await HttpClient.GetAsync($"/users/{user1.Id}/meetings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meetings = await DeserializeResponse<List<MeetingDto>>(response);
        Assert.NotNull(meetings);
        Assert.Single(meetings);

        var meeting = meetings[0];
        Assert.Equal(3, meeting.Participants.Count);

        var participantNames = meeting.Participants.Select(p => p.Name).ToList();
        Assert.Contains("Alice", participantNames);
        Assert.Contains("Bob", participantNames);
        Assert.Contains("Charlie", participantNames);
    }
}