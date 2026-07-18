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

    public async Task<UserStats?> GetAsync(int userId, int mode)
    {
        const string query =
            "SELECT user_id AS UserId, mode AS Mode, ranked_score AS RankedScore, " +
            "total_score AS TotalScore, pp AS Pp, accuracy AS Accuracy, playcount AS Playcount, " +
            "playtime AS Playtime, total_hits AS TotalHits, max_combo AS MaxCombo, " +
            "replays_watched AS ReplaysWatched, level AS Level, count_ssh AS CountSsh, " +
            "count_ss AS CountSs, count_sh AS CountSh, count_s AS CountS, count_a AS CountA " +
            "FROM user_stats WHERE user_id = @userId AND mode = @mode";

        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserStats>(query, new { userId, mode });
    }

    public async Task ClearCustomBadgeAsync(int userId)
    {
        const string query =
            "UPDATE user_settings SET can_custom_badge = 0, show_custom_badge = 0 WHERE user_id = @userId";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, new { userId });
    }
}
