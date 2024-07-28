namespace QuinnlyticsConsole.Models;

public class RoleStats
{
    public string Role { get; set; }
    public int GameCount { get; set; }
    public double WinRatio { get; set; }
    public double KDA { get; set; }
    public string MostFrequentOpponent { get; set; }
    public string AverageGameDuration { get; set; }
    public float MinionsPerMinute { get; set; }
}