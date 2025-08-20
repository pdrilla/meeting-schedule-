using Application.DTOs;
using Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Web.Api.IntegrationTests.MeetingScheduler;

public class ScheduleMeetingEndpointTests : BaseIntegrationTest
{
    public ScheduleMeetingEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task ScheduleMeeting_WithValidRequest_ShouldReturnCreatedMeeting()
    {
        // Arrange
        // First create users
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Two"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        var request = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60, // 1 hour
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), // 10:00 AM
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)  // 4:00 PM
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var meetingDto = await DeserializeResponse<MeetingDto>(response);
        Assert.NotNull(meetingDto);
        Assert.True(meetingDto.Id > 0);
        Assert.Equal(2, meetingDto.Participants.Count);
        Assert.Contains(meetingDto.Participants, p => p.Name == "User One");
        Assert.Contains(meetingDto.Participants, p => p.Name == "User Two");

        // Verify the meeting is scheduled within business hours
        var startTime = meetingDto.StartTime.TimeOfDay;
        var endTime = meetingDto.EndTime.TimeOfDay;
        Assert.True(startTime >= new TimeSpan(9, 0, 0)); // 9:00 AM
        Assert.True(endTime <= new TimeSpan(17, 0, 0));  // 5:00 PM

        // Verify duration
        var duration = meetingDto.EndTime - meetingDto.StartTime;
        Assert.Equal(TimeSpan.FromMinutes(60), duration);
    }

    [Fact]
    public async Task ScheduleMeeting_WithNonExistentParticipants_ShouldReturnNotFound()
    {
        // Arrange
        var request = new ScheduleMeetingRequest(
            new List<int> { 999, 1000 }, // Non-existent user IDs
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleMeeting_WithInvalidDuration_ShouldReturnBadRequest()
    {
        // Arrange
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user1 = await DeserializeResponse<UserDto>(user1Response);
        Assert.NotNull(user1);

        var request = new ScheduleMeetingRequest(
            new List<int> { user1.Id },
            5, // Invalid duration (less than 15 minutes)
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("15 minutes", content);
    }

    [Fact]
    public async Task ScheduleMeeting_WithEmptyParticipantList_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ScheduleMeetingRequest(
            new List<int>(), // Empty participant list
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("participant", content.ToLower());
    }

    [Fact]
    public async Task ScheduleMeeting_WithTimeRangeOutsideBusinessHours_ShouldReturnBadRequest()
    {
        // Arrange
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user1 = await DeserializeResponse<UserDto>(user1Response);
        Assert.NotNull(user1);

        var request = new ScheduleMeetingRequest(
            new List<int> { user1.Id },
            60,
            new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc), // Before business hours
            new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("business hours", content.ToLower());
    }

    [Fact]
    public async Task ScheduleMeeting_WithConflictingMeetings_ShouldHandleConflicts()
    {
        // Arrange
        // Create users
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user2Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User Two"));

        var user1 = await DeserializeResponse<UserDto>(user1Response);
        var user2 = await DeserializeResponse<UserDto>(user2Response);

        Assert.NotNull(user1);
        Assert.NotNull(user2);

        // Schedule first meeting
        var firstMeetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        var firstMeetingResponse = await HttpClient.PostAsJsonAsync("/meetings", firstMeetingRequest);
        Assert.Equal(HttpStatusCode.Created, firstMeetingResponse.StatusCode);

        // Try to schedule conflicting meeting for the same user
        var conflictingMeetingRequest = new ScheduleMeetingRequest(
            new List<int> { user1.Id, user2.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), // Same time range
            new DateTime(2024, 6, 15, 11, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var conflictingResponse = await HttpClient.PostAsJsonAsync("/meetings", conflictingMeetingRequest);

        // Assert
        // The system should either:
        // 1. Find a different time slot (success), or
        // 2. Return an error indicating no available slot
        Assert.True(
            conflictingResponse.StatusCode == HttpStatusCode.Created ||
            conflictingResponse.StatusCode == HttpStatusCode.NotFound ||
            conflictingResponse.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ScheduleMeeting_ShouldPersistToDatabase()
    {
        // Arrange
        var user1Response = await HttpClient.PostAsJsonAsync("/users", new CreateUserRequest("User One"));
        var user1 = await DeserializeResponse<UserDto>(user1Response);
        Assert.NotNull(user1);

        var request = new ScheduleMeetingRequest(
            new List<int> { user1.Id },
            60,
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 16, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync("/meetings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var meetingDto = await DeserializeResponse<MeetingDto>(response);
        Assert.NotNull(meetingDto);

        // Verify meeting is persisted in database
        using var context = await GetDbContextAsync();
        var persistedMeeting = await context.Meetings.FindAsync(meetingDto.Id);
        Assert.NotNull(persistedMeeting);
        Assert.Contains(user1.Id, persistedMeeting.ParticipantIds);
    }
}