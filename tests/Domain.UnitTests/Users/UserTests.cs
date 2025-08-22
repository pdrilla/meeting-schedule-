using Domain.Users;

namespace Domain.UnitTests.Users;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidName_ShouldCreateUser()
    {
        const string name = "John Doe";
        var user = new User(name);
        Assert.Equal(name, user.Name);
        Assert.True(user.Id >= 0);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        const string name = "";
        ArgumentException exception = Assert.Throws<ArgumentException>(static () => new User(name));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        string? name = null;
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new User(name!));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ShouldThrowArgumentException()
    {
        const string name = "   ";
        ArgumentException exception = Assert.Throws<ArgumentException>(static () => new User(name));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
    {
        string name = new('A', 101);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new User(name));
        Assert.Contains("100 characters", exception.Message);
    }

    [Fact]
    public void Constructor_WithMaxLengthName_ShouldCreateUser()
    {
        string name = new('A', 100);
        var user = new User(name);
        Assert.Equal(name, user.Name);
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        var user = new User("Original Name");
        const string newName = "Updated Name";
        user.UpdateName(newName);
        Assert.Equal(newName, user.Name);
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        var user = new User("Original Name");
        const string newName = "";
        ArgumentException exception = Assert.Throws<ArgumentException>(() => user.UpdateName(newName));
        Assert.Contains("name cannot be empty", exception.Message);
    }

    [Fact]
    public void UpdateName_WithNameTooLong_ShouldThrowArgumentException()
    {
        var user = new User("Original Name");
        string newName = new('B', 101);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => user.UpdateName(newName));
        Assert.Contains("100 characters", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldRaiseDomainEvent()
    {
        const string name = "John Doe";
        var user = new User(name);
        Assert.Single(user.DomainEvents);
        SharedKernel.IDomainEvent domainEvent = user.DomainEvents.First();
        Assert.IsType<UserCreatedDomainEvent>(domainEvent);

        var userCreatedEvent = (UserCreatedDomainEvent)domainEvent;
        Assert.Equal(user.Id, userCreatedEvent.UserId);
        Assert.Equal(name, userCreatedEvent.Name);
    }
}

