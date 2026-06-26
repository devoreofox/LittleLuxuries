namespace LittleLuxuries.Tweaks;

public class CommendQueue : Tweak
{
    public override string Name => "Commend Queue";
    public override string Description => "Lets you queue the player you want to commend during a duty, swapping or cancelling your pick at any time, then awards the commendation automatically when the duty ends.";
    public override bool IsImplemented => false;
    public override void DrawConfig() { }
}
