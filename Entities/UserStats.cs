namespace RealistikOsu.Cron.Entities;

public class UserStats
{
    public required int UserId { get; init; }
    public required int Mode { get; init; }
    public required long RankedScore { get; init; }
    public required long TotalScore { get; init; }
    public required long Pp { get; init; }
    public required float Accuracy { get; init; }
    public required int Playcount { get; init; }
    public required int Playtime { get; init; }
    public required long TotalHits { get; init; }
    public required int MaxCombo { get; init; }
    public required int ReplaysWatched { get; init; }
    public required int Level { get; init; }
    public required int CountSsh { get; init; }
    public required int CountSs { get; init; }
    public required int CountSh { get; init; }
    public required int CountS { get; init; }
    public required int CountA { get; init; }
}
