using System.Diagnostics;
using QuinnlyticsConsole.Services;

var apiKey = "";
var gameName = "";
var tagLine = "";

var riotApiService = new RiotApiService(apiKey);
await riotApiService.InitializeAsync();

var puuid = await riotApiService.GetSummonerPuuidAsync(gameName, tagLine);
Console.WriteLine("Fetching match IDs...");
Stopwatch stopwatch = Stopwatch.StartNew();
var matchIds = await riotApiService.GetMatchIdsByPuuidAsync(puuid);
stopwatch.Stop();
Console.WriteLine($"Found {matchIds.Count} match IDs in {stopwatch.ElapsedMilliseconds} ms.");

var databaseService = new DatabaseService();

var runeDict = await riotApiService.GetRunesReforgedAsync();

var latestVersion = await riotApiService.GetCurrentGameVersionAsync();
var gameVersion = string.Join(".", latestVersion.Split('.').Take(3));

foreach (var matchId in matchIds)
{
    Console.WriteLine($"Checking if match {matchId} is in the database...");
    if (!await databaseService.IsMatchInDatabaseAsync(matchId))
    {
        Console.WriteLine($"Match {matchId} not in database. Fetching match data...");
        var match = await riotApiService.GetMatchAsync(matchId, runeDict, puuid);
        Console.WriteLine($"Saving match {matchId} to database...");
        await databaseService.SaveMatchAsync(match);
        Console.WriteLine($"Saved match {matchId} to database");
    }
    else
    {
        Console.WriteLine($"Match {matchId} is already in the database.");
    }
    Console.WriteLine("Done processing matches.");

    Console.WriteLine("Fetching and displaying statistics...");

    // Pobieranie i wyświetlanie statystyk dla najnowszej wersji gry
    var roleStats = await databaseService.GetRoleStatsAsync(latestVersion);
    
    Console.WriteLine($"Game version: {gameVersion}");
    Console.WriteLine("ROLE | Game Count | Win Ratio | KDA | Most Frequent Opponent | Avg Game Duration | Avg CS/min");
    foreach (var stats in roleStats)
    {
        Console.WriteLine($"{stats.Role} | {stats.GameCount} | {stats.WinRatio:P2} | {stats.KDA:F2} | {stats.MostFrequentOpponent} | {stats.AverageGameDuration} | {stats.MinionsPerMinute:F2}");
    }

    Console.WriteLine("Statistics display complete.");
}