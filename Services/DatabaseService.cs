using Microsoft.EntityFrameworkCore;
using QuinnlyticsConsole.Models;

namespace QuinnlyticsConsole.Services;

public class DatabaseService
{
    private readonly AppDbContext _context;

    public DatabaseService()
    {
        _context = new AppDbContext();
        _context.Database.EnsureCreated();
    }

    public async Task SaveMatchAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsMatchInDatabaseAsync(string matchId)
    {
        return await _context.Matches.AnyAsync(m => m.MatchId == matchId);
    }

    public async Task<List<RoleStats>> GetRoleStatsAsync(string gameVersion)
    {
        var roleStats = await _context.Matches
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
}