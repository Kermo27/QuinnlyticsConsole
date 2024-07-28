namespace QuinnlyticsConsole.Models;

public class Rune
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public string ShortDesc { get; set; }
    public string LongDesc { get; set; }
    public List<Slot> Slots { get; set; }
}