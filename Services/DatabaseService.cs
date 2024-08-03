using Microsoft.EntityFrameworkCore;
using QuinnlyticsConsole.Models;

namespace QuinnlyticsConsole.Services;

public class DatabaseService
{
    private readonly AppDbContext _dbContext;

    public DatabaseService()
    {
        _dbContext = new AppDbContext();
        _dbContext.Database.EnsureCreated();
    }

    public async Task<GameVersion> GetCurrentGameVersionAsync()
    {
        return await _dbContext.GameVersions.FirstOrDefaultAsync();
    }
    
    public async Task SaveOrUpdateGameVersionAsync(GameVersion gameVersion)
    {
        _dbContext.GameVersions.Add(gameVersion);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateGameVersionAsync(GameVersion gameVersion)
    {
        _dbContext.GameVersions.Update(gameVersion);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveMatchAsync(Match match)
    {
        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveItemsAsync(IEnumerable<Item> items)
    {
        var existingItems = _dbContext.Items.ToList();
        var newItems = items.Where(i => !existingItems.Any(e => e.Id == i.Id)).ToList();
        
        _dbContext.Items.AddRange(newItems);
        await _dbContext.SaveChangesAsync();
    }

    public Task UpdateItemsAsync(Item item)
    {
        _dbContext.Update(item);
        return Task.CompletedTask;
    }
    
    public async Task<Item> GetItemByIdAsync(int id)
    {
        return await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<bool> IsMatchInDatabaseAsync(string matchId)
    {
        return await _dbContext.Matches.AnyAsync(m => m.MatchId == matchId);
    }

    public async Task<List<RoleStats>> GetRoleStatsAsync(string gameVersion)
    {
        var roleStats = await _dbContext.Matches
            .Where(m => m.GameVersion.StartsWith(gameVersion))
            .GroupBy(m => m.Role)
            .Select(g => new RoleStats
            {
                Role = g.Key,
                GameCount = g.Count(),
                WinRatio = g.Average(m => m.Win ? 1 : 0),
                KDA = (double)g.Sum(m => m.Kills + m.Assists) / (g.Sum(m => m.Deaths) == 0 ? 1 : g.Sum(m => m.Deaths)),
                MostFrequentOpponent = g.GroupBy(m => m.Opponent)
                    .OrderByDescending(op => op.Count())
                    .Select(op => op.Key)
                    .FirstOrDefault() ?? "Unknown",
                AverageGameDuration = TimeSpan.FromSeconds(g.Average(m => m.GameDuration)).ToString(@"hh\:mm\:ss"),
                MinionsPerMinute = g.Average(m => m.MinionsPerMinutes)
            })
            .ToListAsync();

        foreach (var stat in roleStats)
        {
            if (stat.Role == "UTILITY")
            {
                stat.Role = "SUPPORT";
            }
        }

        return roleStats;
    }

    public async Task<Dictionary<string, double>> GetRolePercentageAsync(string gameVersion)
    {
        var matches = await _dbContext.Matches
            .Where(m => m.GameVersion.StartsWith(gameVersion))
            .ToListAsync();

        var totalMatches = matches.Count;

        var rolePercentages = matches
            .GroupBy(m => m.Role)
            .Select(g => new
            {
                Role = g.Key,
                Percentage = (double)g.Count() / totalMatches
            })
            .ToDictionary(x => x.Role, x => x.Percentage);

        return rolePercentages;
    }
    
    public async Task<List<Match>> GetAllMatchesAsync(string gameVersion)
    {
        return await _dbContext.Matches
            .Where(m => m.GameVersion == gameVersion)
            .ToListAsync();
    }

    public async Task<string> GetItemNameByIdAsync(int itemId)
    {
        var item = await _dbContext.Items.FindAsync(itemId);
        return item?.Name ?? "Unknown";
    }
    
    public async Task<Dictionary<string, Dictionary<int, string>>> GetMostPopularItemsBySlotAsync(string gameVersion)
    {
        var matches = await GetAllMatchesAsync(gameVersion);
        var roleBuilds = new Dictionary<string, List<string>>();

        // Grupujemy buildy według roli
        foreach (var match in matches)
        {
            if (!roleBuilds.ContainsKey(match.Role))
            {
                roleBuilds[match.Role] = new List<string>();
            }

            roleBuilds[match.Role].Add(match.Build);
        }

        var mostPopularItems = new Dictionary<string, Dictionary<int, string>>();

        foreach (var roleBuild in roleBuilds)
        {
            var role = roleBuild.Key;
            var builds = roleBuild.Value;

            var slotItemCounts = new Dictionary<int, Dictionary<string, int>>();

            // Inicjalizacja słowników dla każdego slotu
            for (int i = 1; i <= 6; i++)
            {
                slotItemCounts[i] = new Dictionary<string, int>();
            }

            foreach (var build in builds)
            {
                var items = ParseBuild(build);

                for (int i = 0; i < items.Count; i++)
                {
                    var slot = i + 1;
                    var item = items[i];

                    if (slot > 6)
                    {
                        Console.WriteLine($"Invalid slot detected: {slot}. Build: {build}");
                        continue;
                    }

                    if (!slotItemCounts[slot].ContainsKey(item))
                    {
                        slotItemCounts[slot][item] = 0;
                    }

                    slotItemCounts[slot][item]++;
                }
            }

            // Zbieranie najpopularniejszych przedmiotów dla roli
            var rolePopularItems = new Dictionary<int, string>();

            for (int slot = 1; slot <= 6; slot++)
            {
                if (slotItemCounts.ContainsKey(slot))
                {
                    var itemCounts = slotItemCounts[slot];
                    var mostPopularItem = itemCounts.OrderByDescending(x => x.Value).FirstOrDefault().Key;
                    rolePopularItems[slot] = mostPopularItem;
                }
                else
                {
                    rolePopularItems[slot] = "None";
                }
            }

            mostPopularItems[role] = rolePopularItems;
        }

        return mostPopularItems;
    }

    public List<string> ParseBuild(string build)
    {
        // Opcjonalne: Zdebuguj przedmioty
        var items = build.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
        return items;
    }
}