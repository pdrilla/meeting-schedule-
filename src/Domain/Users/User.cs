using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Represents an application user.
/// </summary>
public sealed class User : Entity
{
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the display name of the user.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    private User()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="name">Display name for the user.</param>
    public User(string name)
    {
        ValidateName(name);
        Name = name;

        Raise(new UserCreatedDomainEvent(Id, name));
    }

    /// <summary>
    /// Updates the user's display name.
    /// </summary>
    /// <param name="name">The new name to assign.</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name cannot be empty");
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("User name cannot exceed 100 characters");
        }
    }
}
