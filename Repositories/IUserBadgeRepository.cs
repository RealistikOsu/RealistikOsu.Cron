namespace RealistikOsu.Cron.Repositories;

public interface IUserBadgeRepository
{
    Task DeleteAsync(int userId, int badgeId);
}