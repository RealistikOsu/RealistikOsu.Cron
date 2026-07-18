namespace RealistikOsu.Cron.Entities;

public class FirstPlace
{
    public required string BeatmapMd5 { get; init; }
    public required int Mode { get; init; }         // combined 0-7
    public required long ScoreId { get; init; }
    public required int UserId { get; init; }
    public required double PerformancePoints { get; init; }
}
