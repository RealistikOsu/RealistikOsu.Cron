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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFirstPlaceRepository, FirstPlaceRepository>();
builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();