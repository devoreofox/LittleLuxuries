// Tweaks/DeterministicPosing.cs
namespace LittleLuxuries.Tweaks;

public class DeterministicPosing : Tweak
{
    public override string Name => "Deterministic Posing";
    public override string Description => "Extends the /cpose command to accept an index, allowing you to jump directly to a specific pose rather than cycling through them one at a time. For example, /cpose 3 immediately sets your third standing pose.";
    public override bool IsImplemented => false;
    public override void DrawConfig() { }
}
