using RealistikOsu.Cron;
using RealistikOsu.Cron.Context;
using RealistikOsu.Cron.Repositories;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
builder.Configuration.AddEnvironmentVariables();

var redisConnectionMultiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
{
    EndPoints = { "localhost" }
});
builder.Services.AddSingleton(redisConnectionMultiplexer);

builder.Services.AddSingleton<DapperContext>();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IFirstPlaceRepository, FirstPlaceRepository>();
builder.Services.AddSingleton<IScoreRepository, ScoreRepository>();
builder.Services.AddSingleton<IUserBadgeRepository, UserBadgeRepository>();
builder.Services.AddSingleton<IUserStatsRepository, UserStatsRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();