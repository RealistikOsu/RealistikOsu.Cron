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
            "UPDATE users_stats SET can_custom_badge = @CanCustomBadge, show_custom_badge = @ShowCustombadge, pp_std = @standardPerformancePoints, " +
            "pp_mania = @maniaPerformancePoints, pp_ctb = @catchPerformancePoints, pp_taiko = @taikoPerformancePoints WHERE id = @Id";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, userStats);
    }

    private async Task<UserStats?> FindInTable(int userId, string table)
    {
        string query = $"SELECT * FROM {table} WHERE id = @userId";

        using var connection = _dbContext.CreateConnection();
        var user = (await connection.QueryAsync<dynamic>(query, new { userId }))
            .Select(item => new UserStats
        {
            Id = item.id,
            CanCustomBadge = item.can_custom_badge ?? false, // Botch for rx and vn which dont have these fields.
            ShowCustomBadge = item.show_custom_badge ?? false,
            standardPerformancePoints = item.pp_std,
            taikoPerformancePoints = item.pp_taiko,
            catchPerformancePoints = item.pp_ctb,
            maniaPerformancePoints = item.pp_mania
        }).SingleOrDefault();

        return user;
    }

    private async Task<UserStats> GetFromTable(int userId, string table) =>
        await FindInTable(userId, table) ?? throw new InvalidOperationException($"No row in {table} for user {userId}");

    public async Task<UserStats> GetVanillaUserAsync(int userId) => await GetFromTable(userId, "users_stats");

    public async Task<UserStats> GetRelaxUserAsync(int userId) => await GetFromTable(userId, "rx_stats");

    public async Task<UserStats> GetAutopilotUserAsync(int userId) => await GetFromTable(userId, "ap_stats");

    // Players who never played lazer have no row.
    private async Task<UserStats> GetLazerFromTable(int userId, string table) =>
        await FindInTable(userId, table) ?? new UserStats
        {
            Id = userId,
            CanCustomBadge = false,
            ShowCustomBadge = false,
            standardPerformancePoints = 0,
            taikoPerformancePoints = 0,
            catchPerformancePoints = 0,
            maniaPerformancePoints = 0
        };

    public async Task<UserStats> GetLazerUserAsync(int userId) => await GetLazerFromTable(userId, "lazer_stats");

    public async Task<UserStats> GetLazerRelaxUserAsync(int userId) => await GetLazerFromTable(userId, "lazer_rx_stats");

    public async Task<UserStats> GetLazerAutopilotUserAsync(int userId) => await GetLazerFromTable(userId, "lazer_ap_stats");
}