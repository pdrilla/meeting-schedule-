using Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Web.Api.IntegrationTests.MeetingScheduler;

public class CreateUserEndpointTests : BaseIntegrationTest
{
    public CreateUserEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ShouldReturnCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var userDto = await DeserializeResponse<UserDto>(response);
        Assert.NotNull(userDto);
        Assert.Equal("John Doe", userDto.Name);
        Assert.True(userDto.Id > 0);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/users/{userDto.Id}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateUser_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest("");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content.ToLower());
    }

    [Fact]
    public async Task CreateUser_WithNameTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var longName = new string('A', 101); // 101 characters
        var request = new CreateUserRequest(longName);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("100 characters", content);
    }

    [Fact]
    public async Task CreateUser_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("/users", (CreateUserRequest?)null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = CreateJsonContent("invalid json");

        // Act
        var response = await HttpClient.PostAsync("/users", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithValidName_ShouldPersistToDatabase()
    {
        // Arrange
        var request = new CreateUserRequest("Jane Smith");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var userDto = await DeserializeResponse<UserDto>(response);
        Assert.NotNull(userDto);

        // Verify user is persisted in database
        using var context = await GetDbContextAsync();
        var persistedUser = await context.Users.FindAsync(userDto.Id);
        Assert.NotNull(persistedUser);
        Assert.Equal("Jane Smith", persistedUser.Name);
    }

    [Fact]
    public async Task CreateUser_MultipleUsers_ShouldCreateUniqueIds()
    {
        // Arrange
        var request1 = new CreateUserRequest("User One");
        var request2 = new CreateUserRequest("User Two");

        // Act
        var response1 = await HttpClient.PostAsJsonAsync("/users", request1);
        var response2 = await HttpClient.PostAsJsonAsync("/users", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var user1 = await DeserializeResponse<UserDto>(response1);
        var user2 = await DeserializeResponse<UserDto>(response2);

        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotEqual(user1.Id, user2.Id);
        Assert.Equal("User One", user1.Name);
        Assert.Equal("User Two", user2.Name);
    }
}