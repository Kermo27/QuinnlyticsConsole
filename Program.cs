using QuinnlyticsConsole;
using QuinnlyticsConsole.Services;
using QuinnlyticsConsole.Models;

const string apiKey = "RGAPI-24bffdb3-2ad7-4c70-8b38-e2e2c2ac44d2";
const string gameName = "Кермо";
const string tagLine = "AIBOT";

var databaseService = new DatabaseService();

var customGameVersion = "14.14";

var exceptions = new HashSet<int>
{
    3006, // Berserker's Greaves
    3010 // Symbiotic Soles
};

var excludedItems = new HashSet<int>
{
    2003, // Health Potion
    2055, // Control Ward
    1102, // Gustwalker Hatchling
    1101, // Scorchclaw Pup
    1103, // Mosstomper Seedling
    3363, // Farsight Alteration
    3364, // Oracle Lens
    3340, // Stealth Ward
    2056, // Stealth Ward
    2140, // Elixir of Wrath
    2138, // Elixir of Iron
    2139, // Elixir of Sorcery
    223172, // Zephyr
    3172, // Zephyr
    3865, // World Atlas
};

var riotApiService = new RiotApiService(apiKey);
try
{
    await riotApiService.InitializeAsync();
}
catch (Exception ex)
{
    Console.WriteLine("Failed to initialize Riot API service: " + ex.Message);
    return;
}

string puuid;
try
{
    puuid = await riotApiService.GetSummonerPuuidAsync(gameName, tagLine);
    Console.WriteLine("Retrieved PUUID: " + puuid);
}
catch (HttpRequestException)
{
    Console.WriteLine("Failed to retrieve PUUID. Riot API may be unavailable.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving PUUID: " + ex.Message);
    return;
}

Dictionary<int, Rune> runeDict;
try
{
    runeDict = await riotApiService.GetRunesReforgedAsync();
}
catch (HttpRequestException)
{
    Console.WriteLine("Failed to retrieve rune data. Riot API may be unavailable.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving rune data: " + ex.Message);
    return;
}

string latestVersion;
try
{
    latestVersion = await riotApiService.GetCurrentGameVersionShortAsync();
}
catch (HttpRequestException)
{
    Console.WriteLine("Failed to retrieve current game version. Riot API may be unavailable.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving game version: " + ex.Message);
    return;
}

try
{
    await riotApiService.RefreshItemsIfVersionChangedAsync(databaseService, exceptions, excludedItems);
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while refreshing item data: " + ex.Message);
}

List<string> matchIds;
try
{
    matchIds = await riotApiService.GetMatchIdsByPuuidAsync(puuid);
}
catch (HttpRequestException)
{
    Console.WriteLine("Failed to retrieve match IDs. Riot API may be unavailable.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving match IDs: " + ex.Message);
    return;
}

foreach (var matchId in matchIds)
{
    try
    {
        if (!await databaseService.IsMatchInDatabaseAsync(matchId))
        {
            Console.WriteLine("New match found: " + matchId);
            var match = await riotApiService.GetMatchWithBuildAsync(matchId, runeDict, puuid);
            await databaseService.SaveMatchAsync(match);
        }
    }
    catch (HttpRequestException)
    {
        Console.WriteLine($"Failed to retrieve data for match ID {matchId}. Riot API may be unavailable.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while processing match ID {matchId}: " + ex.Message);
    }
}

Console.WriteLine();

List<RoleStats> roleStats;
try
{
    roleStats = await databaseService.GetRoleStatsAsync(latestVersion);
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving role stats: " + ex.Message);
    return;
}
    
Console.WriteLine($"Game version: {latestVersion}");
Console.WriteLine("ROLE | Game Count | Win Ratio | KDA | Most Frequent Opponent | Avg Game Duration | Avg CS/min");
foreach (var stats in roleStats)
{
    Console.WriteLine($"{stats.Role} | {stats.GameCount} | {stats.WinRatio:P2} | {stats.KDA:F2} | {stats.MostFrequentOpponent} | {stats.AverageGameDuration} | {stats.MinionsPerMinute:F2}");
}
    
Dictionary<string, double> rolePercentage;
try
{
    rolePercentage = await databaseService.GetRolePercentageAsync(latestVersion);
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while retrieving role percentage: " + ex.Message);
    return;
}
    
Console.WriteLine();
    
Console.WriteLine("ROLE | GAMECOUNT | Percentage");
foreach (var (role, percentage) in rolePercentage)
{
    var gameCount = roleStats.First(s => s.Role == role).GameCount;
    Console.WriteLine($"{role} | {gameCount} | {percentage:P2}");
}

Console.WriteLine();

await DisplayMostPopularItemsBySlotAsync();

Console.ReadLine();

async Task DisplayMostPopularItemsBySlotAsync()
{
    var mostPopularItems = await databaseService.GetMostPopularItemsBySlotAsync(latestVersion);

    foreach (var role in mostPopularItems.Keys)
    {
        Console.WriteLine($"Role: {role}");
        foreach (var slot in mostPopularItems[role].Keys)
        {
            Console.WriteLine($"Slot {slot}: {mostPopularItems[role][slot]}");
        }
    }
}
