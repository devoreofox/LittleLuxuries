// Tweaks/PersonalEstateLabels.cs
namespace LittleLuxuries.Tweaks;

public class PersonalEstateLabels : Tweak
{
    public override string Name => "Personal Estate Labels";
    public override string Description => "Allows you to assign custom nicknames to shared estates and apartments in the teleport menu, making it easier to identify them at a glance.";
    public override bool IsImplemented => false;
    public override void DrawConfig() { }
}
