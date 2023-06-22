namespace RealistikOsu.Cron.Entities;

public class Score
{
    public required int Id { get; init; }
    public required int UserId { get; init; }
    public required long PlayScore { get; init; }
    public required int MaxCombo { get; init; }
    public required bool FullCombo { get; init; }
    public required int Mods { get; init; }
    public required int Count300 { get; init; }
    public required int Count100 { get; init; }
    public required int Count50 { get; init; }
    public required int CountKatu { get; init; }
    public required int CountGeki { get; init; }
    public required int CountMiss { get; init; }
    public required int SubmittedAt { get; init; }
    public required int Mode { get; init; }
    public required int Completed { get; init; }
    public required float Accuracy { get; init; }
    public required float PerformancePoints { get; init; }
    public required int PlayTime { get; init; }
    public required string BeatmapMd5 { get; init; }
    public required int Relax { get; init; }
}