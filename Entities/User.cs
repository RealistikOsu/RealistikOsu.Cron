namespace RealistikOsu.Cron.Entities;

public class User
{
    public int Id { get; init; }
    public required string Username { get; set; }
    public required string CountryCode { get; set; }
    public required Privileges Privileges { get; set; }
    public required int LatestActivity { get; set; }
    public required string? BanReason { get; set; }
    public required int? BannedAt { get; set; }
    public required int? DonorExpiresAt { get; set; }
    public required bool Frozen { get; set; }
    public required int? FreezeExpiresAt { get; set; }
}