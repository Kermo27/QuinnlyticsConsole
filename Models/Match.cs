namespace QuinnlyticsConsole.Models;

public class Match
{
    public int Id { get; set; }
    public long MatchDate { get; set; }
    public string MatchId { get; set; }
    public string Role { get; set; }
    public bool Win { get; set; }
    public string Opponent { get; set; }
    public string SummonerSpells { get; set; }
    public string Champion { get; set; }
    public string GameVersion { get; set; }
    public long GameDuration { get; set; }
    public string RuneDetails { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int TotalMinionsKilled { get; set; }
    public float MinionsPerMinutes { get; set; }
    public int QSkillUsage { get; set; }
    public int WSkillUsage { get; set; }
    public int ESkillUsage { get; set; }
    public int RSkillUsage { get; set; }
    public int? AllInPings { get; set; } // Yellow crossed swords
    public int? AssistMePings { get; set; } // Green flag
    public int? CommandPings { get; set; } // Blue generic ping (ALT+click)
    public int? EnemyMissingPings { get; set; } // Yellow questionamark
    public int? EnemyVisionPings { get; set; } // Red eyeball
    public int? GetBackPings { get; set; } // Yellow circle with horizontal line
    public int? NeedVisionPings { get; set; } // Green ward
    public int? OnMyWayPings { get; set; } // Blue arrow pointing at ground
    public int? PushPings { get; set; } // Green minion
    public int GoldEarned { get; set; }
    public int GoldSpent { get; set; }
    
    
}