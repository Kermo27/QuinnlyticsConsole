using Newtonsoft.Json;
using Camille.RiotGames;
using QuinnlyticsConsole.Models;

namespace QuinnlyticsConsole.Services;

public class RiotApiService
{
    private readonly RiotGamesApi _riotGamesApi;
    private readonly string _apiKey;
    private string _currentGameVersion;

    public RiotApiService(string apiKey)
    {
        _apiKey = apiKey;
        _riotGamesApi = RiotGamesApi.NewInstance(apiKey);
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Fetching current game version...");
        _currentGameVersion = await GetCurrentGameVersionAsync();
        Console.WriteLine($"Current game version: {_currentGameVersion}");
    }

    public async Task<string> GetCurrentGameVersionAsync()
    {
        var url = "https://ddragon.leagueoflegends.com/api/versions.json";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(url);
        var versions = JsonConvert.DeserializeObject<string[]>(response);
        return versions[0];
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

    public async Task<Match> GetMatchAsync(string matchId, Dictionary<int, Rune> runeDict, string puuid)
    {
        var region = Camille.Enums.RegionalRoute.EUROPE;
        var match = await _riotGamesApi.MatchV5().GetMatchAsync(region, matchId);

        var participant = match.Info.Participants.First(p => p.Puuid == puuid);

        var gameVersion = await GetCurrentGameVersionAsync();

        var matchEntity = new Match
        {
            MatchId = matchId,
            Role = participant.TeamPosition == "UTILITY" ? "SUPPORT" : participant.TeamPosition,
            Win = participant.Win,
            Build = string.Join(", ", new int[]
                {
                    participant.Item0, participant.Item1, participant.Item2, participant.Item3, participant.Item4,
                    participant.Item5, participant.Item6
                }
                .Where(itemId => itemId != 0) // Ignorowanie pustych slotów
                .Select(itemId => itemId.ToString())), // Build
            Opponent = match.Info.Participants
                .FirstOrDefault(op => op.TeamPosition == participant.TeamPosition && op.TeamId != participant.TeamId)
                ?.ChampionName ?? "Unknown", // Ustawiamy "Unknown" jeśli Opponent jest null
            SummonerSpells = $"Summoner1: {participant.Summoner1Id}, Summoner2: {participant.Summoner2Id}",
            Champion = participant.ChampionName, // Ustawiamy champion name
            GameVersion = gameVersion,
            GameDuration = match.Info.GameDuration,
            RuneDetails = string.Join(", ", participant.Perks.Styles.SelectMany(style => style.Selections).Select(
                selection =>
                    runeDict.TryGetValue(selection.Perk, out var rune) ? rune.Name : $"Rune ID: {selection.Perk}")),
            Kills = participant.Kills,
            Deaths = participant.Deaths,
            Assists = participant.Assists,
            TotalMinionsKilled = participant.TotalMinionsKilled + participant.NeutralMinionsKilled,
            MinionsPerMinutes = (participant.TotalMinionsKilled + participant.NeutralMinionsKilled) / (match.Info.GameDuration / 60f),
            QSkillUsage = participant.Spell1Casts,
            WSkillUsage = participant.Spell2Casts,
            ESkillUsage = participant.Spell3Casts,
            RSkillUsage = participant.Spell4Casts
        };
        return matchEntity;
    }

    public async Task<string> GetSummonerPuuidAsync(string gameName, string tagLine)
    {
        var region = Camille.Enums.RegionalRoute.EUROPE;
        var summoner = await _riotGamesApi.AccountV1().GetByRiotIdAsync(region, gameName, tagLine);
        return summoner.Puuid;
    }

    public async Task<List<string>> GetMatchIdsByPuuidAsync(string puuid, int count = 20)
    {
        var region = Camille.Enums.RegionalRoute.EUROPE;
        var matchIds = await _riotGamesApi.MatchV5().GetMatchIdsByPUUIDAsync(region, puuid, count: count);
        var draftMatchIds = new List<string>();

        foreach (var matchId in matchIds)
        {
            var match = await _riotGamesApi.MatchV5().GetMatchAsync(region, matchId);
            if ((int)match.Info.QueueId == 400)
            {
                draftMatchIds.Add(matchId);
            }
        }

        return draftMatchIds;
    }
}