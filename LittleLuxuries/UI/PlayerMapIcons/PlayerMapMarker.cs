using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.MapOverlay;
using KamiToolKit.Nodes;

namespace LittleLuxuries.UI.PlayerMapIcons;

public sealed unsafe class PlayerMapMarker : MapMarkerNode
{
    private static readonly Vector2 MarkerSize = new(10.0f, 10.0f);

    public required int PlayerIndex { get; init; }
    public required Tweaks.PlayerMapIcons Tweak { get; init; }

    private ImGuiImageNode? _base;

    public PlayerMapMarker()
    {
        Size = MarkerSize;
    }

    public void AddLayers(ITextureProvider textureProvider)
    {
        _base = MarkerTexture.CreateNode(textureProvider, MarkerSize, false);
        _base.AttachNode(this);
        MarkerTexture.CreateNode(textureProvider, MarkerSize, true).AttachNode(this);
    }

    protected override void OnUpdate()
    {
        IsVisible = false;

        var local = GameObjectManager.Instance()->Objects.IndexSorted[0].Value;
        if (local is null) return;

        var chara = CharacterManager.Instance()->BattleCharas[PlayerIndex].Value;
        if (chara is null) return;

        var color = Tweak.MarkerColor(chara, local);
        if (color is null) return;

        _base?.MultiplyColor = color.Value;
        Position = new Vector2(chara->Position.X, chara->Position.Z);
        TextTooltip = chara->NameString;
        MapId = AgentMap.Instance()->CurrentMapId;
        IsVisible = true;
    }
}
