namespace Domain.Meetings;

public interface IMeetingSchedulerService
{
    Task<DateTime?> FindEarliestAvailableSlotAsync(
        List<int> participantIds,
        int durationMinutes,
        DateTime earliestStart,
        DateTime latestEnd);
}