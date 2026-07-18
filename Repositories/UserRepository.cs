using Dapper;
using RealistikOsu.Cron.Context;
using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DapperContext _dbContext;

    public UserRepository(DapperContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<User>> GetAllAsync()
    {
        const string query =
            "SELECT id, username, country AS CountryCode, privileges, public AS Public, " +
            "latest_activity AS LatestActivity, donor_end AS DonorEnd FROM users";

        using var connection = _dbContext.CreateConnection();
        var users = (await connection.QueryAsync<dynamic>(query))
            .Select(item => new User
        {
            Id = (int)item.id,
            Username = item.username,
            CountryCode = item.CountryCode,
            Privileges = (Privileges)(long)item.privileges,
            LatestActivity = item.LatestActivity,
            Public = (bool)item.Public,
            DonorEnd = item.DonorEnd
        });

        return users.ToList();
    }

    public async Task UpdateAsync(User user)
    {
        const string query =
            "UPDATE users SET username = @Username, country = @CountryCode, " +
            "privileges = @Privileges, latest_activity = @LatestActivity, " +
            "public = @Public, donor_end = @DonorEnd WHERE id = @Id";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, user);
    }

    // Users whose active freeze (type 3) has expired.
    public async Task<IEnumerable<int>> GetExpiredFrozenUserIdsAsync()
    {
        const string query =
            "SELECT user_id FROM infractions " +
            "WHERE active = 1 AND type = 3 AND expires_at IS NOT NULL AND expires_at < NOW()";

        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<int>(query);
    }

    // Restrict = insert a restrict infraction + hide the user + deactivate the freeze.
    public async Task RestrictForExpiredFreezeAsync(int userId, int botId)
    {
        using var connection = _dbContext.CreateConnection();

        await connection.ExecuteAsync(
            "INSERT INTO infractions (user_id, moderator_id, type, reason, active, created_at) " +
            "VALUES (@u, @m, 0, 'Expired freeze timer (Cron)', 1, NOW())",
            new { u = userId, m = botId });

        await connection.ExecuteAsync(
            "UPDATE users SET public = 0 WHERE id = @u",
            new { u = userId });

        await connection.ExecuteAsync(
            "UPDATE infractions SET active = 0 WHERE user_id = @u AND active = 1 AND type = 3",
            new { u = userId });
    }
}
