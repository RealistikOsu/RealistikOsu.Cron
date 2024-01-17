using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IUserStatsRepository
{
    Task UpdateAsync(UserStats userStats);
    Task<UserStats?> GetVanillaUserAsync(int  userId);
    Task<UserStats?> GetRelaxUserAsync(int userId);
    Task<UserStats?> GetAutopilotUserAsync(int userId);
}