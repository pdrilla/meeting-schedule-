using Domain.Meetings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Database;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ILogger<ApplicationDbContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if we already have data
            if (await context.MeetingUsers.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            // Seed meeting users
            var users = new List<MeetingUser>
            {
                new("Alice Johnson"),
                new("Bob Smith"),
                new("Carol Davis"),
                new("David Wilson"),
                new("Eve Brown")
            };

            context.MeetingUsers.AddRange(users);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with {UserCount} users", users.Count);

            // Optionally seed some sample meetings for testing
            var sampleMeetings = new List<Meeting>
            {
                // Today's meetings
                new([1, 2],
                    DateTime.UtcNow.Date.AddHours(10), // 10:00 AM today
                    DateTime.UtcNow.Date.AddHours(11)), // 11:00 AM today
                
                new([2, 3, 4],
                    DateTime.UtcNow.Date.AddHours(14), // 2:00 PM today
                    DateTime.UtcNow.Date.AddHours(15)), // 3:00 PM today
                
                // Tomorrow's meetings
                new([1, 3],
                    DateTime.UtcNow.Date.AddDays(1).AddHours(9), // 9:00 AM tomorrow
                    DateTime.UtcNow.Date.AddDays(1).AddHours(10)), // 10:00 AM tomorrow
                
                new([4, 5],
                    DateTime.UtcNow.Date.AddDays(1).AddHours(11), // 11:00 AM tomorrow
                    DateTime.UtcNow.Date.AddDays(1).AddHours(12)), // 12:00 PM tomorrow
                
                // Back-to-back meetings for testing
                new([1],
                    DateTime.UtcNow.Date.AddDays(2).AddHours(13), // 1:00 PM day after tomorrow
                    DateTime.UtcNow.Date.AddDays(2).AddHours(14)), // 2:00 PM day after tomorrow
                
                new([1],
                    DateTime.UtcNow.Date.AddDays(2).AddHours(14), // 2:00 PM day after tomorrow
                    DateTime.UtcNow.Date.AddDays(2).AddHours(15)), // 3:00 PM day after tomorrow
            };

            context.Meetings.AddRange(sampleMeetings);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with {MeetingCount} sample meetings", sampleMeetings.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw new InvalidOperationException("Database seeding failed. See inner exception for details.", ex);
        }
    }
}