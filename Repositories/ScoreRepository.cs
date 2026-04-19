using Dapper;
using RealistikOsu.Cron.Context;
using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public class ScoreRepository : IScoreRepository
{
    private readonly DapperContext _dbContext;

    public ScoreRepository(DapperContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Score?> FindBestAsync(string beatmapMd5, int relax, int mode)
    {
        var table = relax switch
        {
            1 => "scores_relax",
            2 => "scores_ap",
            _ => "scores"
        };

        var sort = relax switch
        {
            1 or 2 => "pp",
            _ => "score"
        };

        var query =
            $@"SELECT s.*, `300_count` as count_300, `100_count` as count_100, `50_count` as count_50 FROM {table} s INNER JOIN users ON users.id = s.userid 
                WHERE s.beatmap_md5 = @BeatmapMd5 AND s.play_mode = @Mode AND s.completed = 3 AND users.privileges & 1 ORDER BY {sort} DESC LIMIT 1";
        
        using var connection = _dbContext.CreateConnection();
        var bestScore = await connection.QuerySingleOrDefaultAsync<dynamic>(query, new { BeatmapMd5 = beatmapMd5, Mode = mode });
        if (bestScore is null)
            return null;

        return new Score
        {
            Id = bestScore.id,
            UserId = bestScore.userid,
            PlayScore = bestScore.score,
            MaxCombo = bestScore.max_combo,
            FullCombo = (bool)bestScore.full_combo,
            Mods = bestScore.mods,
            Count300 = bestScore.count_300,
            Count100 = bestScore.count_100,
            Count50 = bestScore.count_50,
            CountKatu = bestScore.katus_count,
            CountGeki = bestScore.gekis_count,
            CountMiss = bestScore.misses_count,
            SubmittedAt = int.Parse(bestScore.time),
            Mode = bestScore.play_mode,
            Completed = bestScore.completed,
            Accuracy = bestScore.accuracy,
            PerformancePoints = bestScore.pp,
            PlayTime = bestScore.playtime,
            BeatmapMd5 = bestScore.beatmap_md5
        };
    }
}