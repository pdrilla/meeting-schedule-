using SharedKernel;

namespace Domain.Meetings;

public sealed class MeetingUser : Entity
{
    public int Id { get; }
    public string Name { get; private set; } = string.Empty;

    private MeetingUser()
    {
    }

    public MeetingUser(string name)
    {
        ValidateName(name);
        Name = name;

        Raise(new MeetingUserCreatedDomainEvent(Id, name));
    }

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