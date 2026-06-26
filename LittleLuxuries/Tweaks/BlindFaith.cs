namespace LittleLuxuries.Tweaks;

public class BlindFaith : Tweak
{
    public override string Name => "Blind Faith";
    public override string Description => "Removes every other player's character model while you're inside any Leap of Faith course, clearing the crowd so you can actually see the platforms and line up your jumps. Models return the moment you leave the instance.";
    public override bool IsImplemented => false;
    public override void DrawConfig() { }
}
