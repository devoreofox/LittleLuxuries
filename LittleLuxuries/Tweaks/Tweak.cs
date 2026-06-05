namespace LittleLuxuries.Tweaks;

public abstract class Tweak
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool IsImplemented => true;
    public abstract void DrawConfig();

}
