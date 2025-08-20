using Domain.Meetings;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<MeetingUser> MeetingUsers { get; }
    DbSet<Meeting> Meetings { get; }
    DbSet<TodoItem> TodoItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
