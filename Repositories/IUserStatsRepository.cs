using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IUserStatsRepository
{
    Task<UserStats?> GetAsync(int userId, int mode);
    Task ClearCustomBadgeAsync(int userId);
}
