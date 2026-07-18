namespace RealistikOsu.Cron.Entities;

[Flags]
public enum Privileges
{
    Activated = 1,
    Donor = 2,
    ModManageUsers = 4,
    ModViewRapLogs = 8,
    ModManageReports = 16,
    ModManageClans = 32,
    AdminSendAlerts = 64,
    AdminManageSettings = 128,
    AdminManageBadges = 256,
    AdminManagePrivileges = 512,
    DevViewErrorLogs = 1024,
    TournamentStaff = 2048,
    Bot = 4096,
    BnStd = 8192,
    BnTaiko = 16384,
    BnCtb = 32768,
    BnMania = 65536,
}
