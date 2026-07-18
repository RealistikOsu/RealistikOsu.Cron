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
        int combined = CombinedMode(mode, relax); // vanilla 0-3, relax 4-6, autopilot 7

        const string query =
            "SELECT s.id, s.user_id AS UserId, s.score AS PlayScore, s.max_combo AS MaxCombo, " +
            "s.full_combo AS FullCombo, s.mods AS Mods, s.count_300 AS Count300, s.count_100 AS Count100, " +
            "s.count_50 AS Count50, s.count_katu AS CountKatu, s.count_geki AS CountGeki, " +
            "s.count_miss AS CountMiss, UNIX_TIMESTAMP(s.submitted_at) AS SubmittedAt, s.mode AS Mode, " +
            "s.status AS Completed, s.accuracy AS Accuracy, s.pp AS PerformancePoints, " +
            "s.playtime AS PlayTime, s.beatmap_md5 AS BeatmapMd5 " +
            "FROM scores s INNER JOIN users ON users.id = s.user_id " +
            "WHERE s.beatmap_md5 = @beatmapMd5 AND s.mode = @combined AND s.status = 2 AND users.public = 1 " +
            "ORDER BY s.pp DESC LIMIT 1";

        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Score>(query, new { beatmapMd5, combined });
    }

    private static int CombinedMode(int mode, int relax) =>
        relax == 2 ? 7 : relax == 1 ? 4 + mode : mode;
}
