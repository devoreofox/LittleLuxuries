// Tweaks/PartyFinderCleanup.cs
namespace LittleLuxuries.Tweaks;

public class PartyFinderCleanup : Tweak
{
    public override string Name => "Party Finder Cleanup";
    public override string Description => "Removes duplicate listings from the Party Finder's \"Other\" tab, reducing visual noise when multiple identical entries are posted.";
    public override bool IsImplemented => false;
    public override void DrawConfig() { }
}
