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
        const string query = "SELECT * FROM users";

        using var connection = _dbContext.CreateConnection();
        var users = (await connection.QueryAsync<dynamic>(query))
            .Select(item => new User
        {
            Id = item.id,
            Username = item.username,
            CountryCode = item.country,
            Privileges = (Privileges)item.privileges,
            LatestActivity = item.latest_activity,
            BanReason = item.ban_reason,
            BannedAt = item.ban_datetime,
            DonorExpiresAt = item.donor_expire,
            Frozen = (bool)item.frozen,
            FreezeExpiresAt = item.freezedate
        });

        return users.ToList();
    }

    public async Task UpdateAsync(User user)
    {
        const string query =
            @"UPDATE users SET id = @Id, username = @Username, country = @CountryCode, privileges = @Privileges, latest_activity = @LatestActivity, 
            ban_datetime = @BannedAt, donor_expire = @DonorExpiresAt, frozen = @Frozen, freezedate = @FreezeExpiresAt, ban_reason = @BanReason WHERE id = @Id";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, user);
    }
}