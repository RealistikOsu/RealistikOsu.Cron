FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RealistikOsu.Cron.csproj", "./"]
RUN dotnet restore "RealistikOsu.Cron.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "RealistikOsu.Cron.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RealistikOsu.Cron.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RealistikOsu.Cron.dll"]
