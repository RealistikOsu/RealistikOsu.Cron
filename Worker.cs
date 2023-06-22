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

        var firstPlace = new FirstPlace
        {
            ScoreId = newBest.Id,
            UserId = newBest.UserId,
            Score = newBest.PlayScore,
            MaxCombo = newBest.MaxCombo,
            FullCombo = newBest.FullCombo,
            Mods = newBest.Mods,
            Count300 = newBest.Count300,
            Count100 = newBest.Count100,
            Count50 = newBest.Count50,
            CountKatu = newBest.CountKatu,
            CountGeki = newBest.CountGeki,
            CountMiss = newBest.CountMiss,
            SubmittedAt = newBest.SubmittedAt,
            Mode = newBest.Mode,
            Completed = newBest.Completed,
            Accuracy = newBest.Accuracy,
            PerformancePoints = newBest.PerformancePoints,
            PlayTime = newBest.PlayTime,
            BeatmapMd5 = newBest.BeatmapMd5,
            Relax = newBest.Relax
        };
        await _firstPlaceRepository.CreateAsync(firstPlace);
    }
    
    private async Task RestrictExpiredFrozenUsers(IEnumerable<User> frozenUsers)
    {
        var usersToRestrict = frozenUsers.Where(user => user.FreezeExpiresAt < DateTimeOffset.Now.ToUnixTimeSeconds()).ToArray();

        foreach (var user in usersToRestrict)
        {
            user.Privileges &= ~Privileges.Public;
            user.BannedAt = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            user.BanReason = "Expired freeze timer (Cron)";
            await _userRepository.UpdateAsync(user);

            var parameters = new Dictionary<string, string>
            {
                ["k"] = _fokaKey,
                ["to"] = user.Username,
                ["msg"] =
                    "Your account has been restricted! Check with staff to see whats up." // matching panel message
            };
            await SendFokabotMessage(parameters);

            await RemoveUserFromLeaderboard(user.Id, user.CountryCode);
            await NotifyBan(user.Id);

            var firstPlaces = await _firstPlaceRepository.GetAllByUserAsync(user.Id);
            foreach (var firstPlace in firstPlaces)
            {
                await _firstPlaceRepository.DeleteAsync(firstPlace);
                await RecalculateFirstPlace(firstPlace.BeatmapMd5, firstPlace.Relax, firstPlace.Mode);
            }

            _logger.LogDebug("Restricted {user} ({user_id}) as their freeze timer expired at {time}", user.Username, user.Id, user.FreezeExpiresAt);
        }
        
        _logger.LogInformation("Restricted {count} users for expired freeze timers", usersToRestrict.Length);
    }

    private async Task RemoveExpiredDonors(IEnumerable<User> donors)
    {
        var expiredDonors = donors.Where(user => user.DonorExpiresAt < DateTimeOffset.Now.ToUnixTimeSeconds()).ToArray();

        foreach (var user in expiredDonors)
        {
            user.Privileges &= ~Privileges.Donor;
            await _userRepository.UpdateAsync(user);

            await _userBadgeRepository.DeleteAsync(user.Id, _donorBadgeId);

            var userStats = new UserStats
            {
                Id = user.Id,
                CanCustomBadge = false,
                ShowCustomBadge = false
            };
            await _userStatsRepository.UpdateAsync(userStats);

            _logger.LogDebug("Removed donor from {user} ({user_id}) as their donor expired at {time}", user.Username, user.Id, user.DonorExpiresAt);
        }
        
        _logger.LogInformation("Removed donor from {count} users as their donor expired", expiredDonors.Length);
    }

    private async Task RemoveInactiveUsersFromLeaderboard(IEnumerable<User> inactiveUsers)
    {
        foreach (var inactiveUser in inactiveUsers)
        {
            await RemoveUserFromLeaderboard(inactiveUser.Id, inactiveUser.CountryCode);
            _logger.LogDebug("Removed {user} ({user_id}) from leaderboards due to inactivity", inactiveUser.Username, inactiveUser.Id);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var users = await _userRepository.GetAllAsync();
            var donors = users.Where(user => user.Privileges.HasFlag(Privileges.Donor));
            var frozenUsers = users.Where(user => user.Frozen);
            var inactiveUsers = users.Where(user =>
                user.LatestActivity < (DateTimeOffset.Now - TimeSpan.FromDays(60)).ToUnixTimeSeconds() && !user.Privileges.HasFlag(Privileges.PendingVerification));

            await Task.WhenAll(
                RemoveExpiredDonors(donors), 
                RestrictExpiredFrozenUsers(frozenUsers),
                RemoveInactiveUsersFromLeaderboard(inactiveUsers)
            );

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
