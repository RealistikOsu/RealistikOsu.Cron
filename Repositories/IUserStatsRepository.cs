using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IUserStatsRepository
{
    Task UpdateAsync(UserStats userStats);
}