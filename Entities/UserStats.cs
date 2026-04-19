namespace RealistikOsu.Cron.Entities;

public class UserStats
{
    public required int Id { get; init; }
    public required bool CanCustomBadge { get; set; }
    public required bool ShowCustomBadge { get; set; }
    public required float standardPerformancePoints { get; set; }
    public required float taikoPerformancePoints { get; set; }
    public required float catchPerformancePoints { get; set; }
    public required float maniaPerformancePoints { get; set; }
}