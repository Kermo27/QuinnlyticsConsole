using Microsoft.EntityFrameworkCore;
using QuinnlyticsConsole.Models;

namespace QuinnlyticsConsole;

public class AppDbContext : DbContext
{
    public DbSet<Match> Matches { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=leagueData.db");
    }
}