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
        const string query = "SELECT *, `300_count` as count_300, `100_count` as count_100, `50_count` as count_50 FROM first_places";

        using var connection = _dbContext.CreateConnection();
        var firstPlaces = (await connection.QueryAsync<dynamic>(query))
            .Select(item => new FirstPlace
            {
                Id = item.id,
                ScoreId = item.score_id,
                UserId = item.user_id,
                Score = item.score,
                MaxCombo = item.max_combo,
                FullCombo = (bool)item.full_combo,
                Mods = item.mods,
                Count300 = item.count_300,
                Count100 = item.count_100,
                Count50 = item.count_50,
                CountKatu = item.ckatus_count,
                CountGeki = item.cgekis_count,
                CountMiss = item.miss_count,
                SubmittedAt = item.timestamp,
                Mode = item.mode,
                Completed = item.completed,
                Accuracy = item.accuracy,
                PerformancePoints = item.pp,
                PlayTime = item.play_time,
                BeatmapMd5 = item.beatmap_md5,
                Relax = item.relax,
            });

        return firstPlaces.ToList();
    }

    public async Task CreateAsync(FirstPlace firstPlace)
    {
        const string query =
            @"INSERT INTO first_places (score_id, user_id, score, max_combo, full_combo, mods, `300_count`, `100_count`, `50_count`, 
                          ckatus_count, cgekis_count, miss_count, timestamp, mode, completed, accuracy, pp, play_time, beatmap_md5, relax) 
            VALUES (@ScoreId, @UserId, @Score, @MaxCombo, @FullCombo, @Mods, @Count300, @Count100, @Count50, @CountKatu, @CountGeki, @CountMiss, 
                    @SubmittedAt, @Mode, @Completed, @Accuracy, @PerformancePoints, @PlayTime, @BeatmapMd5, @Relax)";
        
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, firstPlace);
    }

    public async Task DeleteAsync(FirstPlace firstPlace)
    {
        const string query = "DELETE FROM first_places WHERE id = @Id";

        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(query, firstPlace);
    }
}