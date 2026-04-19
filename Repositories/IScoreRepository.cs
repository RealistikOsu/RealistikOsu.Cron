using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IScoreRepository
{
    Task<Score?> FindBestAsync(string beatmapMd5, int relax, int mode);
}