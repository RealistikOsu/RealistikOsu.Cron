namespace RealistikOsu.Cron.Entities;

public class UserStats
{
    public required int Id { get; init; }
    public required bool CanCustomBadge { get; set; }
    public required bool ShowCustomBadge { get; set; }
}