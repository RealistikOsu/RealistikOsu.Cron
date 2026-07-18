using Dapper;
using RealistikOsu.Cron.Context;
using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public class FirstPlaceRepository : IFirstPlaceRepository
{
    private readonly DapperContext _dbContext;

    public FirstPlaceRepository(DapperContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<FirstPlace>> GetAllByUserAsync(int userId)
    {
        const string query =
            "SELECT beatmap_md5 AS BeatmapMd5, mode AS Mode, score_id AS ScoreId, " +
            "user_id AS UserId, pp AS PerformancePoints " +
            "FROM first_places WHERE user_id = @userId";

        using var connection = _dbContext.CreateConnection();
        var firstPlaces = await connection.QueryAsync<FirstPlace>(query, new { userId });

        return firstPlaces.ToList();
    }

    public async Task CreateAsync(FirstPlace firstPlace)
    {
        const string query =
            "REPLACE INTO first_places (beatmap_md5, mode, score_id, user_id, pp) " +
            "VALUES (@BeatmapMd5, @Mode, @ScoreId, @UserId, @PerformancePoints)";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, firstPlace);
    }

    public async Task DeleteAsync(FirstPlace firstPlace)
    {
        const string query =
            "DELETE FROM first_places WHERE beatmap_md5 = @BeatmapMd5 AND mode = @Mode";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, firstPlace);
    }
}
