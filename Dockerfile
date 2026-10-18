FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RealistikOsu.Cron.csproj", "./"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "RealistikOsu.Cron.csproj"
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "RealistikOsu.Cron.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RealistikOsu.Cron.dll"]
