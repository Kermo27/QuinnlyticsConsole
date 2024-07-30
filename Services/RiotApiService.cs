using Camille.Enums;
using Newtonsoft.Json;
using Camille.RiotGames;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using QuinnlyticsConsole.Models;

namespace QuinnlyticsConsole.Services;

public class RiotApiService
{
    private readonly RiotGamesApi _riotGamesApi;
    private readonly string _apiKey;
    private string? _currentGameVersion;

    private const RegionalRoute Region = RegionalRoute.EUROPE;

    public RiotApiService(string apiKey)
    {
        _apiKey = apiKey;
        _riotGamesApi = RiotGamesApi.NewInstance(apiKey);
    }

    public async Task InitializeAsync()
    {
        _currentGameVersion = await GetCurrentGameVersionLongAsync();
    }

    public async Task<string?> GetCurrentGameVersionLongAsync()
    {
        var url = "https://ddragon.leagueoflegends.com/api/versions.json";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(url);
        string?[]? versions = JsonConvert.DeserializeObject<string[]>(response);
        return versions[0];
    }
    
    public async Task<string> GetCurrentGameVersionShortAsync()
    {
        var url = "https://ddragon.leagueoflegends.com/api/versions.json";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(url);
        var versions = JsonConvert.DeserializeObject<string[]>(response);
        var shortVersion = string.Join(".", versions[0].Split('.').Take(2));
        return shortVersion;
    }

    public async Task<Dictionary<int, Rune>> GetRunesReforgedAsync()
    {
        var url = $"https://ddragon.leagueoflegends.com/cdn/{_currentGameVersion}/data/en_US/runesReforged.json";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(url);
        var runes = JsonConvert.DeserializeObject<List<Rune>>(response);
        return runes.SelectMany(r => r.Slots.SelectMany(s => s.Runes))
            .ToDictionary(r => r.Id);
    }

    public async Task<Match> GetMatchWithBuildAsync(string matchId, Dictionary<int, Rune> runeDictionary, string playerUniqueId)
    {
        using var dbContext = new AppDbContext();
        
        var matchTask = _riotGamesApi.MatchV5().GetMatchAsync(Region, matchId);
        var timelineTask = _riotGamesApi.MatchV5().GetTimelineAsync(Region, matchId);

        await Task.WhenAll(matchTask, timelineTask);

        var match = matchTask.Result;
        var timeline = timelineTask.Result;
        
        var participant = match.Info.Participants.FirstOrDefault(p => p.Puuid == playerUniqueId);
        if (participant == null)
        {
            throw new ArgumentException("Player not found in the match.");
        }

        var participantId = participant.ParticipantId;
        
        var purchases = timeline.Info.Frames
            .SelectMany(frame => frame.Events)
            .Where(e => e.Type == "ITEM_PURCHASED" && e.ParticipantId == participantId)
            .OrderBy(e => e.Timestamp)
            .Select(e => e.ItemId)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        var player = match.Info.Participants.FirstOrDefault(p => p.Puuid == playerUniqueId);
        if (player == null)
        {
            throw new ArgumentException("Player not found in the match.");
        }

        var endGameItems = new List<int>
        {
            player.Item0, player.Item1, player.Item2, player.Item3, player.Item4, player.Item5
        };
        
        var itemIds = purchases.Concat(endGameItems).Distinct().ToList();
        var items = await dbContext.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        var build = purchases
            .Where(endGameItems.Contains)
            .Where(items.ContainsKey)
            .Select(itemId => items[itemId].Name)
            .Take(6)
            .ToList();
        
        var opponent = match.Info.Participants
            .FirstOrDefault(op => op.TeamPosition == player.TeamPosition && op.TeamId != player.TeamId)
            ?.ChampionName ?? "Unknown";
        
        var runeDetails = player.Perks.Styles
            .SelectMany(style => style.Selections)
            .Select(selection => runeDictionary.TryGetValue(selection.Perk, out var rune) ? rune.Name : $"Rune ID: {selection.Perk}")
            .ToArray();

        var matchEntity = new Match
        {
            MatchDate = match.Info.GameCreation,
            MatchId = matchId,
            Role = player.TeamPosition == "UTILITY" ? "SUPPORT" : player.TeamPosition,
            Win = player.Win,
            Opponent = opponent,
            SummonerSpells = $"Summoner1: {player.Summoner1Id}, Summoner2: {player.Summoner2Id}",
            Champion = player.ChampionName,
            GameVersion = string.Join(".", match.Info.GameVersion.Split('.').Take(2)),
            GameDuration = match.Info.GameDuration,
            RuneDetails = string.Join(", ", runeDetails),
            Kills = player.Kills,
            Deaths = player.Deaths,
            Assists = player.Assists,
            TotalMinionsKilled = player.TotalMinionsKilled + player.NeutralMinionsKilled,
            MinionsPerMinutes = (player.TotalMinionsKilled + player.NeutralMinionsKilled) /
                                (match.Info.GameDuration / 60f),
            QSkillUsage = player.Spell1Casts,
            WSkillUsage = player.Spell2Casts,
            ESkillUsage = player.Spell3Casts,
            RSkillUsage = player.Spell4Casts,
            AllInPings = player.AllInPings,
            AssistMePings = player.AssistMePings,
            CommandPings = player.CommandPings,
            EnemyMissingPings = player.EnemyMissingPings,
            EnemyVisionPings = player.EnemyVisionPings,
            GetBackPings = player.GetBackPings,
            NeedVisionPings = player.NeedVisionPings,
            OnMyWayPings = player.OnMyWayPings,
            PushPings = player.PushPings,
            GoldEarned = player.GoldEarned,
            GoldSpent = player.GoldSpent,
            Build = string.Join(", ", build)
        };

        return matchEntity;
    }
    
    public async Task<string> GetSummonerPuuidAsync(string gameName, string tagLine)
    {
        var summoner = await _riotGamesApi.AccountV1().GetByRiotIdAsync(Region, gameName, tagLine);
        return summoner.Puuid;
    }

    public async Task<List<string>> GetMatchIdsByPuuidAsync(string puuid, int count = 10)
    {
        var matchIds = await _riotGamesApi.MatchV5().GetMatchIdsByPUUIDAsync(Region, puuid, count: count);
        var draftMatchIds = new List<string>();

        foreach (var matchId in matchIds)
        {
            var match = await _riotGamesApi.MatchV5().GetMatchAsync(Region, matchId);
            if ((int)match.Info.QueueId == 400 || (int)match.Info.QueueId == 420 || (int)match.Info.QueueId == 440)
            {
                draftMatchIds.Add(matchId);
            }
        }

        return draftMatchIds;
    }

    public async Task FetchAndSaveItemsAsync(DatabaseService dbService, HashSet<int> exceptions, HashSet<int> excludedItems)
    {
        var itemUrl = $"https://ddragon.leagueoflegends.com/cdn/{_currentGameVersion}/data/en_US/item.json";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(itemUrl);
        var itemData = JsonConvert.DeserializeObject<JObject>(response);
        
        var itemsToSave = new List<Item>();

        foreach (var item in itemData["data"].Children<JProperty>())
        {
            var itemId = int.Parse(item.Name);
            var itemDetails = item.Value;
            var itemName = (string)itemDetails["name"];
            var intoArray = itemDetails["into"] as JArray;

            var hasIntoItems = intoArray != null && intoArray.Count > 0;
            var isExcluded = excludedItems.Contains(itemId);
            var isException = exceptions.Contains(itemId);

            if ((!hasIntoItems || isException) && !isExcluded)
            {
                var existingItem = await dbService.GetItemByIdAsync(itemId);
                if (existingItem != null)
                {
                    existingItem.Id = itemId;
                    existingItem.Name = itemName;
                    await dbService.UpdateItemsAsync(existingItem);
                }
                else
                {
                    itemsToSave.Add(new Item
                    {
                        Id = itemId,
                        Name = itemName
                    });
                }
            }
        }

        if (itemsToSave.Any())
        {
            await dbService.SaveItemsAsync(itemsToSave);
        }
    }

    public async Task RefreshItemsIfVersionChangedAsync(DatabaseService dbService, HashSet<int> exceptions, HashSet<int> excludedItems)
    {
        var currentVersion = await GetCurrentGameVersionLongAsync();
        var gameVersionInDb = await dbService.GetCurrentGameVersionAsync();

        if (gameVersionInDb == null || gameVersionInDb.Version != currentVersion)
        {
            if (gameVersionInDb == null)
            {
                gameVersionInDb = new GameVersion { Version = currentVersion };
                await dbService.SaveOrUpdateGameVersionAsync(gameVersionInDb);
            }
            else
            {
                gameVersionInDb.Version = currentVersion;
                await dbService.UpdateGameVersionAsync(gameVersionInDb);
            }

            await FetchAndSaveItemsAsync(dbService, exceptions, excludedItems);
        }
    }
}