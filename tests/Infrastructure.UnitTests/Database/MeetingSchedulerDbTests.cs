using Domain.Meetings;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.UnitTests.Database;

public class MeetingSchedulerDbTests
{
    [Fact]
    public async Task CanCreateAndQueryMeetingUsers()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new MeetingUser("Test User");
        context.MeetingUsers.Add(user);
        await context.SaveChangesAsync();

        var retrievedUser = await context.MeetingUsers.FirstOrDefaultAsync(u => u.Name == "Test User");

        Assert.NotNull(retrievedUser);
        Assert.Equal("Test User", retrievedUser.Name);
    }

    [Fact]
    public async Task CanCreateAndQueryMeetings()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var startTime = DateTime.UtcNow.Date.AddHours(10);
        var endTime = startTime.AddHours(1);
        var meeting = new Meeting([1, 2], startTime, endTime);

        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var retrievedMeeting = await context.Meetings.FirstOrDefaultAsync();

        Assert.NotNull(retrievedMeeting);
        Assert.Equal(startTime, retrievedMeeting.StartTime);
        Assert.Equal(endTime, retrievedMeeting.EndTime);
        Assert.Contains(1, retrievedMeeting.ParticipantIds);
        Assert.Contains(2, retrievedMeeting.ParticipantIds);
    }

    [Fact]
    public async Task MeetingParticipantIdsAreStoredCorrectly()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participantIds = new List<int> { 1, 2, 3, 4, 5 };
        var meeting = new Meeting(
            participantIds,
            DateTime.UtcNow.Date.AddHours(10),
            DateTime.UtcNow.Date.AddHours(11));

        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var retrievedMeeting = await context.Meetings.FirstOrDefaultAsync();

        Assert.NotNull(retrievedMeeting);
        Assert.Equal(5, retrievedMeeting.ParticipantIds.Count);
        Assert.All(participantIds, id => Assert.Contains(id, retrievedMeeting.ParticipantIds));
    }
}