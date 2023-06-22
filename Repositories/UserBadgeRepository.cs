using Dapper;
using RealistikOsu.Cron.Context;

namespace RealistikOsu.Cron.Repositories;

public class UserBadgeRepository : IUserBadgeRepository
{
    private readonly DapperContext _dbContext;

    public UserBadgeRepository(DapperContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task DeleteAsync(int userId, int badgeId)
    {
        const string query = "DELETE FROM user_badges WHERE user = @UserId AND badge = @BadgeId";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, new { UserId = userId, BadgeId = badgeId });
    }
}