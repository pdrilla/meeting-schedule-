using Domain.Users;

namespace Domain.UnitTests.Users;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidName_ShouldCreateUser()
    {
        // Arrange
        const string name = "John Doe";

        // Act
        var user = new User(name);

        // Assert
        Assert.Equal(name, user.Name);
        Assert.True(user.Id >= 0); // ID should be assigned
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        const string name = "";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(static () => new User(name));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        string? name = null;

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new User(name!));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        const string name = "   ";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(static () => new User(name));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        string name = new('A', 101); // 101 characters

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new User(name));
        Assert.Contains("100 characters", exception.Message);
    }

    [Fact]
    public void Constructor_WithMaxLengthName_ShouldCreateUser()
    {
        // Arrange
        string name = new('A', 100); // Exactly 100 characters

        // Act
        var user = new User(name);

        // Assert
        Assert.Equal(name, user.Name);
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var user = new User("Original Name");
        const string newName = "Updated Name";

        // Act
        user.UpdateName(newName);

        // Assert
        Assert.Equal(newName, user.Name);
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User("Original Name");
        const string newName = "";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => user.UpdateName(newName));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void UpdateName_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User("Original Name");
        string newName = new('B', 101); // 101 characters

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => user.UpdateName(newName));
        Assert.Contains("100 characters", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldRaiseDomainEvent()
    {
        // Arrange
        const string name = "John Doe";

        // Act
        var user = new User(name);

        // Assert
        Assert.Single(user.DomainEvents);
        SharedKernel.IDomainEvent domainEvent = user.DomainEvents.First();
        Assert.IsType<UserCreatedDomainEvent>(domainEvent);

        var userCreatedEvent = (UserCreatedDomainEvent)domainEvent;
        Assert.Equal(user.Id, userCreatedEvent.UserId);
        Assert.Equal(name, userCreatedEvent.Name);
    }
}