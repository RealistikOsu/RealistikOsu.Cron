namespace RealistikOsu.Cron.Entities;

public class User
{
    public int Id { get; init; }
    public required string Username { get; set; }
    public required string CountryCode { get; set; }
    public required Privileges Privileges { get; set; }
    public required DateTime? LatestActivity { get; set; }
    public required bool Public { get; set; }
    public required DateTime? DonorEnd { get; set; }
}
