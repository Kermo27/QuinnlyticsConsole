using QuinnlyticsConsole.Services;

const string apiKey = "RGAPI-24bffdb3-2ad7-4c70-8b38-e2e2c2ac44d2";
const string gameName = "Кермо";
const string tagLine = "AIBOT";

var exceptions = new HashSet<string>
{
    "3006",
    "3010"
};

var riotApiService = new RiotApiService(apiKey);
await riotApiService.InitializeAsync();

var puuid = await riotApiService.GetSummonerPuuidAsync(gameName, tagLine);
var matchIds = await riotApiService.GetMatchIdsByPuuidAsync(puuid);

var databaseService = new DatabaseService();

var runeDict = await riotApiService.GetRunesReforgedAsync();

var latestVersion = await riotApiService.GetCurrentGameVersionShortAsync();

await riotApiService.RefreshItemsIfVersionChangedAsync(databaseService, exceptions);

foreach (var matchId in matchIds)
{
    if (!await databaseService.IsMatchInDatabaseAsync(matchId))
    {
        var match = await riotApiService.GetMatchAsync(matchId, runeDict, puuid);
        await databaseService.SaveMatchAsync(match);
    }
}

Console.WriteLine();

var roleStats = await databaseService.GetRoleStatsAsync(latestVersion);
    
Console.WriteLine($"Game version: {latestVersion}");
Console.WriteLine("ROLE | Game Count | Win Ratio | KDA | Most Frequent Opponent | Avg Game Duration | Avg CS/min");
foreach (var stats in roleStats)
{
    Console.WriteLine($"{stats.Role} | {stats.GameCount} | {stats.WinRatio:P2} | {stats.KDA:F2} | {stats.MostFrequentOpponent} | {stats.AverageGameDuration} | {stats.MinionsPerMinute:F2}");
}
    
var rolePercentage = await databaseService.GetRolePercentageAsync(latestVersion);
    
Console.WriteLine();
    
Console.WriteLine("ROLE | GAMECOUNT | Percentage");
foreach (var (role, percentage) in rolePercentage)
{
    Console.WriteLine($"{role} | {roleStats.First(s => s.Role == role).GameCount} | {percentage:P2}");
}
