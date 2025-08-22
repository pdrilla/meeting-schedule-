using System.Reflection;
using Application.Abstractions.Repositories;
using Application.DTOs;
using Application.MeetingScheduler.Meetings.GetUserMeetings;
using Domain.Meetings;
using Domain.Users;
using Moq;
using SharedKernel;

namespace Application.UnitTests.MeetingScheduler.Meetings.GetUserMeetings;

public class GetUserMeetingsQueryHandlerTests
{
    private readonly Mock<IMeetingRepository> _mockMeetingRepository;
    private readonly Mock<IMeetingUserRepository> _mockUserRepository;
    private readonly GetUserMeetingsQueryHandler _handler;

    public GetUserMeetingsQueryHandlerTests()
    {
        _mockMeetingRepository = new Mock<IMeetingRepository>();
        _mockUserRepository = new Mock<IMeetingUserRepository>();

        _handler = new GetUserMeetingsQueryHandler(
            _mockMeetingRepository.Object,
            _mockUserRepository.Object);
    }

    private static MeetingUser CreateUser(int id, string name)
    {
        var user = new MeetingUser(name);
        typeof(MeetingUser).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(user, id);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnUserMeetings()
    {
        const int userId = 1;
        var query = new GetUserMeetingsQuery(userId);
        var user = CreateUser(1, "John Doe");

        var meetings = new List<Meeting>
        {
            new([1, 2], new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc)),
            new([1, 3], new DateTime(2024, 1, 15, 14, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 15, 15, 0, 0, DateTimeKind.Utc))
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMeetingRepository
            .Setup(r => r.GetUserMeetingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        _mockUserRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateUser(1, "John Doe"),
                CreateUser(2, "Jane"),
                CreateUser(3, "Bob")
            ]);

        Result<List<MeetingDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), result.Value[0].StartTime);
        Assert.Equal(new DateTime(2024, 1, 15, 14, 0, 0, DateTimeKind.Utc), result.Value[1].StartTime);
    }

    [Fact]
    public async Task Handle_WithNonExistentUserId_ShouldReturnFailureResult()
    {
        const int userId = 999;
        var query = new GetUserMeetingsQuery(userId);

        _mockUserRepository
            .Setup(static r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingUser?)null);

        Result<List<MeetingDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound(userId), result.Error);
    }

    [Fact]
    public async Task Handle_WithUserWithNoMeetings_ShouldReturnEmptyList()
    {
        const int userId = 1;
        var query = new GetUserMeetingsQuery(userId);
        var user = CreateUser(1, "John Doe");

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMeetingRepository
            .Setup(static r => r.GetUserMeetingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<List<MeetingDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithValidUserId_ShouldCallRepositoryMethods()
    {
        const int userId = 1;
        var query = new GetUserMeetingsQuery(userId);
        var user = CreateUser(1, "John Doe");
        var meetings = new List<Meeting>();

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMeetingRepository
            .Setup(r => r.GetUserMeetingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        await _handler.Handle(query, CancellationToken.None);

        _mockUserRepository.Verify(
            static r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMeetingRepository.Verify(
            static r => r.GetUserMeetingsAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepositories()
    {
        const int userId = 1;
        var query = new GetUserMeetingsQuery(userId);
        var user = CreateUser(1, "John Doe");
        var meetings = new List<Meeting>();
        var cancellationToken = new CancellationToken();

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMeetingRepository
            .Setup(r => r.GetUserMeetingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        await _handler.Handle(query, cancellationToken);

        _mockUserRepository.Verify(
            r => r.GetByIdAsync(userId, cancellationToken),
            Times.Once);

        _mockMeetingRepository.Verify(
            r => r.GetUserMeetingsAsync(userId, cancellationToken),
            Times.Once);
    }
}
