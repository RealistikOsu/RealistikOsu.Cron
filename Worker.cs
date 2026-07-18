using RealistikOsu.Cron.Entities;
using RealistikOsu.Cron.Helpers;
using RealistikOsu.Cron.Repositories;
using StackExchange.Redis;

namespace RealistikOsu.Cron;

public class Worker : BackgroundService
{
    private readonly string _fokaKey;
    private readonly string _banchoApiUrl;
    private readonly int _donorBadgeId;
    private readonly int _botId;

    private readonly HttpClient _httpClient;

    private readonly ILogger<Worker> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ConnectionMultiplexer _redisConnectionMultiplexer;
    private readonly IFirstPlaceRepository _firstPlaceRepository;
    private readonly IScoreRepository _scoreRepository;
    private readonly IUserBadgeRepository _userBadgeRepository;
    private readonly IUserStatsRepository _userStatsRepository;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IUserRepository userRepository,
        ConnectionMultiplexer redisConnectionMultiplexer, IFirstPlaceRepository firstPlaceRepository,
        IScoreRepository scoreRepository, IUserBadgeRepository userBadgeRepository,
        IUserStatsRepository userStatsRepository)
    {
        _fokaKey = configuration.GetValue<string>("FokaKey") ?? string.Empty;
        _banchoApiUrl = configuration.GetValue<string>("BanchoApiUrl")!;
        _donorBadgeId = configuration.GetValue<int>("DonorBadgeId");
        _botId = configuration.GetValue<int?>("BotId") ?? 999;

        _httpClient = new HttpClient();

        _logger = logger;
        _userRepository = userRepository;
        _redisConnectionMultiplexer = redisConnectionMultiplexer;
        _firstPlaceRepository = firstPlaceRepository;
        _scoreRepository = scoreRepository;
        _userBadgeRepository = userBadgeRepository;
        _userStatsRepository = userStatsRepository;
    }

    private static readonly string[] LeaderboardKeys = {
        "ripple:leaderboard:std",
        "ripple:leaderboard:taiko",
        "ripple:leaderboard:ctb",
        "ripple:leaderboard:mania",
        "ripple:leaderboard_relax:std",
        "ripple:leaderboard_relax:taiko",
        "ripple:leaderboard_relax:ctb",
        "ripple:leaderboard_ap:std"
    };

    // Each redis leaderboard maps to a combined mode in the tall user_stats table
    // (vanilla 0-3, relax 4-6, autopilot 7).
    private static readonly Dictionary<string, int> LeaderboardModeLookup = new()
    {
        { "ripple:leaderboard:std", 0 },
        { "ripple:leaderboard:taiko", 1 },
        { "ripple:leaderboard:ctb", 2 },
        { "ripple:leaderboard:mania", 3 },
        { "ripple:leaderboard_relax:std", 4 },
        { "ripple:leaderboard_relax:taiko", 5 },
        { "ripple:leaderboard_relax:ctb", 6 },
        { "ripple:leaderboard_ap:std", 7 },
    };

    private async Task SendFokabotMessage(Dictionary<string, string> parameters)
    {
        var requestUrl = $"{_banchoApiUrl}/api/v1/fokabotMessage";
        var builder = new UriHelper(requestUrl);

        foreach (var kvp in parameters)
        {
            builder.AddParameter(kvp.Key, kvp.Value);
        }

        await _httpClient.GetAsync(builder.Uri);
    }

    private async Task RemoveUserFromLeaderboard(int userId, string countryCode)
    {
        var redis = _redisConnectionMultiplexer.GetDatabase();

        foreach (var leaderboardKey in LeaderboardKeys)
        {
            await redis.SortedSetRemoveAsync(leaderboardKey, userId);

            var countryKey = $"{leaderboardKey}:{countryCode}";
            if (countryCode != "XX")
                await redis.SortedSetRemoveAsync(countryKey, userId);
        }
    }

    private async Task<bool> UserInLeaderboard(int userId)
    {
        var redis = _redisConnectionMultiplexer.GetDatabase();

        // It is possible to be in only one of the leaderboards.
        foreach (var key in LeaderboardKeys)
        {
            var set = await redis.SortedSetRankAsync(key, userId);

            if (set is not null) return true;
        }

        return false;
        /*
        return LeaderboardKeys.All(key =>
        {
            return await redis.SortedSetRankAsync(key, userId) is not null;
        })
        */
    }

    private async Task NotifyBan(int userId)
    {
        var redis = _redisConnectionMultiplexer.GetDatabase();
        await redis.PublishAsync("peppy:ban", userId);
    }

    private async Task RecalculateFirstPlace(string beatmapMd5, int relax, int mode)
    {
        var newBest = await _scoreRepository.FindBestAsync(beatmapMd5, relax, mode);
        if (newBest is null)
            return;

        await _firstPlaceRepository.CreateAsync(new FirstPlace
        {
            BeatmapMd5 = beatmapMd5,
            Mode = CombinedMode(mode, relax),
            ScoreId = newBest.Id,
            UserId = newBest.UserId,
            PerformancePoints = newBest.PerformancePoints,
        });
    }

    // Combined mode: vanilla 0-3, relax 4-6, autopilot 7.
    private static int CombinedMode(int mode, int relax) =>
        relax == 2 ? 7 : relax == 1 ? 4 + mode : mode;

    // first_places.Mode is combined 0-7; split back for RecalculateFirstPlace's (mode, relax) signature.
    private static int RelaxFromMode(int m) => m == 7 ? 2 : m >= 4 ? 1 : 0;
    private static int BaseModeFromMode(int m) => m == 7 ? 0 : m >= 4 ? m - 4 : m;

    private async Task RestrictExpiredFrozenUsers(IEnumerable<int> expiredFrozenIds, IEnumerable<User> allUsers)
    {
        var ids = expiredFrozenIds.ToArray();
        var byId = allUsers.ToDictionary(u => u.Id);

        foreach (var userId in ids)
        {
            // infraction (type 0) + public = 0 + deactivate the freeze row.
            await _userRepository.RestrictForExpiredFreezeAsync(userId, _botId);

            if (byId.TryGetValue(userId, out var user))
            {
                var parameters = new Dictionary<string, string>
                {
                    ["k"] = _fokaKey,
                    ["to"] = user.Username,
                    ["msg"] =
                        "Your account has been restricted! Check with staff to see whats up." // matching panel message
                };
                await SendFokabotMessage(parameters);

                await RemoveUserFromLeaderboard(userId, user.CountryCode);
            }

            await NotifyBan(userId);

            var firstPlaces = await _firstPlaceRepository.GetAllByUserAsync(userId);
            foreach (var firstPlace in firstPlaces)
            {
                await _firstPlaceRepository.DeleteAsync(firstPlace);
                await RecalculateFirstPlace(firstPlace.BeatmapMd5, RelaxFromMode(firstPlace.Mode), BaseModeFromMode(firstPlace.Mode));
            }

            _logger.LogDebug("Restricted user ({user_id}) as their freeze timer expired", userId);
        }

        _logger.LogInformation("Restricted {count} users for expired freeze timers", ids.Length);
    }

    private async Task RemoveExpiredDonors(IEnumerable<User> donors)
    {
        var expiredDonors = donors.ToArray();

        foreach (var user in expiredDonors)
        {
            user.Privileges &= ~Privileges.Donor;
            user.DonorEnd = null;
            await _userRepository.UpdateAsync(user);

            await _userBadgeRepository.DeleteAsync(user.Id, _donorBadgeId);
            await _userStatsRepository.ClearCustomBadgeAsync(user.Id); // cosmetics now in user_settings

            _logger.LogDebug("Removed donor from {user} ({user_id}) as their donor expired", user.Username, user.Id);
        }

        _logger.LogInformation("Removed donor from {count} users as their donor expired", expiredDonors.Length);
    }

    private async Task RemoveInactiveUsersFromLeaderboard(IEnumerable<User> inactiveUsers)
    {
        foreach (var inactiveUser in inactiveUsers)
        {
            var inLeaderboard = await UserInLeaderboard(inactiveUser.Id);
            if (!inLeaderboard)
                continue;

            await RemoveUserFromLeaderboard(inactiveUser.Id, inactiveUser.CountryCode);
            _logger.LogDebug("Removed {user} ({user_id}) from leaderboards due to inactivity", inactiveUser.Username, inactiveUser.Id);
        }
    }

    private async Task FillLeaderboards(IEnumerable<User> users)
    {
        // Probably should be in the repo but it is what it issss...
        var redis = _redisConnectionMultiplexer.GetDatabase();

        foreach (var user in  users)
        {
            foreach (var key in LeaderboardKeys)
            {
                string? countryKey = null;
                if (user.CountryCode != "XX")
                     countryKey = $"{key}:{user.CountryCode}";

                var stats = await _userStatsRepository.GetAsync(user.Id, LeaderboardModeLookup[key]);
                var value = stats?.Pp ?? 0;

                // If we have a zero value, remove them from the lb.
                if (value == 0)
                {
                    await redis.SortedSetRemoveAsync(key, user.Id);

                    if (countryKey is not null) await redis.SortedSetRemoveAsync(countryKey, user.Id);
                    continue;
                }

                await redis.SortedSetAddAsync(key, user.Id, value);
                if (countryKey is not null) await redis.SortedSetAddAsync(countryKey, user.Id, value);
            }
        }
    }

    // Mainly to cleanup country lbs which arent always properly cleaned up.
    private async Task RemoveRestrictedLeaderboards(IEnumerable<User> users)
    {
        var redis = _redisConnectionMultiplexer.GetDatabase();

        var deletedUsers = 0;

        foreach (var user in users)
        {
            foreach (var key in LeaderboardKeys)
            {
                string? countryKey = null;
                if (user.CountryCode != "XX")
                    countryKey = $"{key}:{user.CountryCode}";

                if (await redis.SortedSetRemoveAsync(key, user.Id)) deletedUsers++;
                if (countryKey is not null) await redis.SortedSetRemoveAsync(countryKey, user.Id);

            }
        }
        _logger.LogInformation("Removed {count} restricted users from the leaderboards.", deletedUsers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var users = await _userRepository.GetAllAsync();
            var donors = users.Where(user =>
                user.Privileges.HasFlag(Privileges.Donor) &&
                user.DonorEnd is not null &&
                user.DonorEnd < DateTime.UtcNow);
            var expiredFrozenIds = await _userRepository.GetExpiredFrozenUserIdsAsync();
            var inactiveUsers = users.Where(user =>
                user.LatestActivity is not null &&
                user.LatestActivity < DateTime.UtcNow.AddDays(-60) &&
                user.Privileges.HasFlag(Privileges.Activated)); // not pending

            var unrestrictedUsers = users.Where(user => user.Public);
            var restrictedUsers = users.Where(user => !user.Public);

            await Task.WhenAll(
                RemoveExpiredDonors(donors),
                RestrictExpiredFrozenUsers(expiredFrozenIds, users),
                FillLeaderboards(unrestrictedUsers),
                RemoveRestrictedLeaderboards(restrictedUsers),
                RemoveInactiveUsersFromLeaderboard(inactiveUsers) // was computed but never called; now runs
            );

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
