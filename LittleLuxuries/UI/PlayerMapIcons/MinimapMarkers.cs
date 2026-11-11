using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using Map = Lumina.Excel.Sheets.Map;

namespace LittleLuxuries.UI.PlayerMapIcons;

public sealed unsafe class MinimapMarkers : IDisposable
{
    private const float NaviMapSize = 218.0f;
    private const float NaviMapRadius = 0.315f;
    private const float MarkerSize = 10.0f;

    private readonly Tweaks.PlayerMapIcons tweak;
    private readonly ITextureProvider textureProvider;
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly Configuration configuration;
    private readonly AddonController controller;

    private readonly List<(ResNode Node, ImGuiImageNode Base)> _markers = new();
    private ResNode? _container;

    public MinimapMarkers(Tweaks.PlayerMapIcons tweak, ITextureProvider textureProvider, IClientState clientState, IDataManager dataManager, Configuration configuration)
    {
        this.tweak = tweak;
        this.textureProvider = textureProvider;
        this.clientState = clientState;
        this.dataManager = dataManager;
        this.configuration = configuration;

        controller = new AddonController
        {
            AddonName = "_NaviMap",
            OnSetup = OnSetup,
            OnPreUpdate = OnUpdate,
            OnFinalize = OnFinalize
        };
        controller.Enable();
    }

    public void Dispose() => controller.Dispose();

    private void OnSetup(AtkUnitBase* addon)
    {
        _container = new ResNode { Size = new Vector2(NaviMapSize), IsVisible = true };
        _container.AttachNode(addon);

        for (var i = 0; i < CharacterManager.Instance()->BattleCharas.Length; i++)
        {
            var size = new Vector2(MarkerSize);
            var marker = new ResNode { Size = size, IsVisible = false };
            marker.AttachNode(_container);

            var markerBase = MarkerTexture.CreateNode(textureProvider, size, false);
            markerBase.AttachNode(marker);
            MarkerTexture.CreateNode(textureProvider, size, true).AttachNode(marker);

            _markers.Add((marker, markerBase));
        }
    }

    private void OnFinalize(AtkUnitBase* addon)
    {
        _markers.Clear();
        _container?.Dispose();
        _container = null;
    }

    private void OnUpdate(AtkUnitBase* addon)
    {
        if (_container is null) return;

        var local = GameObjectManager.Instance()->Objects.IndexSorted[0].Value;
        var show = configuration.PlayerMapIcons && !clientState.IsPvP && local is not null;

        var lockNode = addon->GetNodeById(4);
        var rotationNode = addon->GetNodeById(8);
        var zoomNode = addon->GetNodeById(18);
        var lockBox = lockNode == null ? null : (AtkComponentCheckBox*)lockNode->GetComponent();
        var zoomComponent = zoomNode == null ? null : zoomNode->GetComponent();
        var zoomImage = zoomComponent == null ? null : zoomComponent->GetImageNodeById(6);

        _container.IsVisible = show && lockBox != null && rotationNode != null && zoomImage != null;
        if (!_container.IsVisible) return;

        var center = new Vector2(NaviMapSize / 2.0f, NaviMapSize / 2.0f - 5.0f / addon->Scale);
        var player = new Vector2(local->Position.X, local->Position.Z);
        var scale = ZoneScale(AgentMap.Instance()->CurrentMapId) * zoomImage->ScaleX;
        var radius = NaviMapSize * NaviMapRadius;
        var rotate = !lockBox->IsChecked;
        var (sin, cos) = MathF.SinCos(rotationNode->Rotation);

        var characters = CharacterManager.Instance()->BattleCharas;
        for (var i = 0; i < _markers.Count; i++)
        {
            var (marker, markerBase) = _markers[i];
            var chara = characters[i].Value;
            var style = chara is null ? null : tweak.Marker(chara, local);
            if (style is not { Minimap: true })
            {
                marker.IsVisible = false;
                continue;
            }

            var offset = (new Vector2(chara->Position.X, chara->Position.Z) - player) * scale;
            if (rotate) offset = new Vector2(cos * offset.X - sin * offset.Y, sin * offset.X + cos * offset.Y);
            var length = offset.Length();
            if (length > radius) offset *= radius / length;

            marker.Position = center + offset - new Vector2(MarkerSize / 2.0f);
            markerBase.MultiplyColor = style.Value.Color;
            marker.IsVisible = true;
        }
    }

    private float ZoneScale(uint mapId) =>
        dataManager.GetExcelSheet<Map>().GetRowOrDefault(mapId) is { SizeFactor: > 0 } map ? map.SizeFactor / 100.0f : 1.0f;
}
