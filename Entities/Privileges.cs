namespace RealistikOsu.Cron.Entities;

[Flags]
public enum Privileges
{
    Public = 1,
    Normal = 2 << 0,
    Donor = 2 << 1,
    AccessAdminPanel = 2 << 2,
    ManageUsers = 2 << 3,
    BanUsers = 2 << 4,
    SilenceUsers = 2 << 5,
    WipeUsers = 2 << 6,
    ManageBeatmaps = 2 << 7,
    ManageServers = 2 << 8,
    ManageSettings = 2 << 9,
    ManageBetaKeys = 2 << 10,
    ManageReports = 2 << 11,
    ManageDocs = 2 << 12,
    ManageBadges = 2 << 13,
    ViewAdminPanelLogs = 2 << 14,
    ManagePrivileges = 2 << 15,
    SendAlerts = 2 << 16,
    ChatMod = 2 << 17,
    KickUsers = 2 << 18,
    PendingVerification = 2 << 19,
    TournamentStaff = 2 << 20,
    AdminCaker = 2 << 21,
    BotUser = 1 << 30,
}