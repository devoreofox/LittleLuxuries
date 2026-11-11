using System.Numerics;

namespace LittleLuxuries.Models.PlayerMapIcons;

public class MarkerColors
{
    public Vector3 AllPlayers = new(1.0f, 1.0f, 1.0f);
    public Vector3 Friends = new(1.0f, 0.65f, 0.0f);
    public Vector3 FreeCompany = new(0.56f, 0.93f, 0.56f);
    public Vector3 Whitelisted = new(0.93f, 0.51f, 0.93f);
}
