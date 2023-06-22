using Dapper;
using RealistikOsu.Cron.Context;
using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public class UserStatsRepository : IUserStatsRepository
{
    private readonly DapperContext _dbContext;

    public UserStatsRepository(DapperContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateAsync(UserStats userStats)
    {
        const string query =
            "UPDATE users_stats SET can_custom_badge = @CanCustomBadge, show_custom_badge = @ShowCustombadge WHERE id = @Id";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, userStats);
    }
}