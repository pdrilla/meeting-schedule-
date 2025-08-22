using Application.Abstractions.Repositories;
using Application.DTOs;
using Application.MeetingScheduler.Users.CreateUser;
using Domain.Meetings;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel;

namespace Application.UnitTests.MeetingScheduler.Users.CreateUser;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IMeetingUserRepository> _mockUserRepository;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IMeetingUserRepository>();
        var mockLogger = new Mock<ILogger<CreateUserCommandHandler>>();
        _handler = new CreateUserCommandHandler(_mockUserRepository.Object, mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        const string userName = "John Doe";
        var command = new CreateUserCommand(userName);
        var user = new MeetingUser(userName);

        _mockUserRepository
            .Setup(static r => r.AddAsync(It.IsAny<MeetingUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Result<UserDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userName, result.Value.Name);
        Assert.Equal(user.Id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallRepositoryAddAsync()
    {
        const string userName = "Jane Smith";
        var command = new CreateUserCommand(userName);
        var user = new MeetingUser(userName);

        _mockUserRepository
            .Setup(static r => r.AddAsync(It.IsAny<MeetingUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(command, CancellationToken.None);

        _mockUserRepository.Verify(
            static r => r.AddAsync(It.Is<MeetingUser>(static u => u.Name == userName), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainValidationFails_ShouldReturnFailureResult()
    {
        const string emptyName = "";
        var command = new CreateUserCommand(emptyName);

        Result<UserDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmptyName, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNameTooLong_ShouldReturnFailureResult()
    {
        string longName = new string('A', 101);
        var command = new CreateUserCommand(longName);

        Result<UserDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NameTooLong, result.Error);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        const string userName = "John Doe";
        var command = new CreateUserCommand(userName);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<MeetingUser>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        const string userName = "John Doe";
        var command = new CreateUserCommand(userName);
        var user = new MeetingUser(userName);
        var cancellationToken = new CancellationToken();

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<MeetingUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(command, cancellationToken);

        _mockUserRepository.Verify(
            r => r.AddAsync(It.IsAny<MeetingUser>(), cancellationToken),
            Times.Once);
    }
}
