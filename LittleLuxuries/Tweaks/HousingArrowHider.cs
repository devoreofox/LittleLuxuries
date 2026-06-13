using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using LittleLuxuries.Housing;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace LittleLuxuries.Tweaks;

public class HousingArrowHider : Tweak, IDisposable
{
    public override string Name => "Hide Housing Arrows";
    public override string Description => "Hides the interaction arrows that appear in housing areas you own.";

    private readonly INamePlateGui namePlateGui;
    private readonly IClientState clientState;
    private readonly IGameGui gameGui;
    private readonly IFramework framework;
    private readonly FurnishingScanner scanner;
    private readonly Action toggleWhitelistWindow;
    private readonly Configuration configuration;

    private ulong _currentHousingId;
    private Dictionary<uint, string>? _activeWhitelist;

    private readonly HashSet<FurnishingId> _highlighted = new();
    private readonly Dictionary<FurnishingId, int> _highlightIndex = new();
    private const uint HighlightColor = 0xFFFF7FB2;
    private const uint DefaultColor = 0xFFFFFFFF;

    public ulong CurrentHousingId => _currentHousingId;
    public IReadOnlyDictionary<uint, string>? ActiveWhitelist => _activeWhitelist;

    private readonly HashSet<FurnishingId> _untargeted = new();

    private bool _canManage;
    public bool CanManageCurrentHouse => _canManage;

    private bool _zoneDirty;
    private string _blanketFilter = string.Empty;

    private nint _movingAddress;
    private int _pendingFrames;
    private const int PendingPlacementFrames = 30;

    public HousingArrowHider(INamePlateGui namePlateGui, IClientState clientState, IGameGui gameGui, IFramework framework, FurnishingScanner scanner, Action toggleWhitelistWindow, Configuration configuration)
    {
        this.namePlateGui = namePlateGui;
        this.clientState = clientState;
        this.gameGui = gameGui;
        this.framework = framework;
        this.scanner = scanner;
        this.toggleWhitelistWindow = toggleWhitelistWindow;
        this.configuration = configuration;

        namePlateGui.OnDataUpdate += OnDataUpdate;
        framework.Update += OnFrameworkUpdate;
        clientState.TerritoryChanged += OnTerritoryChanged;

        _zoneDirty = true;
    }

    public void Dispose()
    {
        namePlateGui.OnDataUpdate -= OnDataUpdate;
        framework.Update -= OnFrameworkUpdate;

        clientState.TerritoryChanged -= OnTerritoryChanged;
        foreach (var index in _highlightIndex.Values) ResetHighlightColor(index);
        RestoreAllTargetable();
    }

    public void AddToWhitelist(FurnishingId furnishingId, string name)
    {
        if (_currentHousingId == 0 || !_canManage) return;

        if (!configuration.UserWhitelist.TryGetValue(_currentHousingId, out var dict))
        {
            dict = new Dictionary<uint, string>();
            configuration.UserWhitelist[_currentHousingId] = dict;
            _activeWhitelist = dict;
        }

        dict[furnishingId.Value] = name;
        configuration.Save();
        RestoreAllTargetable();
    }

    public void RemoveFromWhitelist(FurnishingId furnishingId)
    {
        if (_activeWhitelist?.Remove(furnishingId.Value) == true) configuration.Save();
    }

    public bool IsHighlighted(FurnishingId furnishingId) => _highlighted.Contains(furnishingId);

    public void ToggleHighlight(FurnishingId furnishingId)
    {
        if (_highlighted.Remove(furnishingId))
        {
            if (_highlightIndex.Remove(furnishingId, out var index)) ResetHighlightColor(index);
            return;
        }
        _highlighted.Add(furnishingId);
        RestoreTargetable(furnishingId);
    }

    private unsafe bool HasHousePermissions()
    {
        var mgr = HousingManager.Instance();
        return mgr != null && mgr->HasHousePermissions();
    }

    private void OnTerritoryChanged(uint territory)
    {
        (_currentHousingId, _activeWhitelist, _canManage) = (0, null, false);
        _zoneDirty = HousingData.TerritoryIds.Contains(territory);
        _untargeted.Clear();
        _highlighted.Clear();
        _highlightIndex.Clear();
        _movingAddress = 0;
        _pendingFrames = 0;
    }

    private unsafe ulong ReadIndoorHouseId()
    {
        var mgr = HousingManager.Instance();
        return mgr != null ? mgr->GetCurrentIndoorHouseId().Id : 0;
    }

    private static unsafe void SetTargetable(nint address, bool targetable)
    {
        var gameObject = (GameObject*)address;
        if (targetable) gameObject->TargetableStatus |= ObjectTargetableFlags.IsTargetable;
        else gameObject->TargetableStatus &= ~ObjectTargetableFlags.IsTargetable;
    }

    private void RestoreIfUntargeted(FurnishingId furnishingId, IGameObject gameObject)
    {
        if (_untargeted.Remove(furnishingId)) SetTargetable(gameObject.Address, true);
    }

    private void RestoreAllTargetable()
    {
        if (_untargeted.Count == 0) return;
        foreach (var furnishing in scanner.Enumerate())
            if (_untargeted.Contains(furnishing.Id)) SetTargetable(furnishing.Address, true);
        _untargeted.Clear();
    }

    private void RestoreTargetable(FurnishingId furnishingId)
    {
        if (!_untargeted.Remove(furnishingId)) return;
        foreach (var furnishing in scanner.Enumerate())
        {
            if (furnishing.Id == furnishingId)
            {
                SetTargetable(furnishing.Address, true);
                break;
            }
        }
    }

    private unsafe void ResetHighlightColor(int namePlateIndex)
    {
        var addon = gameGui.GetAddonByName("NamePlate");
        if (addon.Address == nint.Zero) return;
        var namePlate = (AddonNamePlate*)addon.Address;
        var textNode = namePlate->NamePlateObjectArray[namePlateIndex].NameText;
        if (textNode != null) *(uint*)&textNode->TextColor = DefaultColor;
    }

    private unsafe nint MovingObjectAddress()
    {
        var mgr = HousingManager.Instance();
        if (mgr == null || mgr->IndoorTerritory == null) return nint.Zero;
        return (nint)mgr->IndoorTerritory->MovingHousingObject;
    }

    private void PruneStaleWhitelist()
    {
        if (_activeWhitelist == null) return;
        var present = scanner.Enumerate().Select(f => f.Id.Value).ToHashSet();
        var stale = _activeWhitelist.Keys.Where(k => !present.Contains(k)).ToList();
        foreach (var k in stale) _activeWhitelist.Remove(k);
        if (stale.Count > 0) configuration.Save();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!HousingData.TerritoryIds.Contains(clientState.TerritoryType)) return;

        if (_zoneDirty)
        {
            var id = ReadIndoorHouseId();
            if (id != 0)
            {
                _currentHousingId = id;
                _canManage = HasHousePermissions();
                _activeWhitelist = configuration.UserWhitelist.GetValueOrDefault(id);
                _zoneDirty = false;
            }
        }

        var moving = MovingObjectAddress();
        if (moving != 0)
        {
            _movingAddress = moving;
            return;
        }

        if (_movingAddress != 0)
        {
            _movingAddress = 0;
            _pendingFrames = PendingPlacementFrames;
        }

        if (_pendingFrames > 0 && --_pendingFrames == 0) PruneStaleWhitelist();
    }

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!configuration.HideHousingArrows) return;
        if (!HousingData.TerritoryIds.Contains(clientState.TerritoryType)) return;
        if (!configuration.PreventInteraction) RestoreAllTargetable();

        foreach (var handler in handlers)
        {
            var gameObject = handler.GameObject;
            if (gameObject is null || gameObject.ObjectKind != ObjectKind.HousingEventObject) continue;

            var id = FurnishingId.From(gameObject);

            if (_highlighted.Contains(id))
            {
                _highlightIndex[id] = handler.NamePlateIndex;
                handler.TextColor = HighlightColor;
                RestoreIfUntargeted(id, gameObject);
                continue;
            }
            if (configuration.FurnishingWhitelist.Contains(gameObject.Name.TextValue))
            {
                RestoreIfUntargeted(id, gameObject);
                continue;
            }
            if (_activeWhitelist?.ContainsKey(id.Value) == true)
            {
                RestoreIfUntargeted(id, gameObject);
                continue;
            }

            handler.VisibilityFlags = 0;
            if (configuration.PreventInteraction)
            {
                SetTargetable(gameObject.Address, false);
                _untargeted.Add(id);
            }
        }
    }

    public override void DrawConfig()
    {
        var hideArrows = configuration.HideHousingArrows;
        if (ImGui.Checkbox("Hide Housing Arrows", ref hideArrows))
        {
            configuration.HideHousingArrows = hideArrows;
            configuration.Save();
            if (!hideArrows) RestoreAllTargetable();
        }

        if (!configuration.HideHousingArrows) return;

        ImGui.SameLine();
        var prevent = configuration.PreventInteraction;
        if (ImGui.Checkbox("Prevent Interaction", ref prevent))
        {
            configuration.PreventInteraction = prevent;
            configuration.Save();
            if (!prevent) RestoreAllTargetable();
        }

        if (ImGui.CollapsingHeader("Always Display"))
        {
            ImGui.Spacing();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##blanketFilter", "Search...", ref _blanketFilter, 100);
            ImGui.Spacing();

            foreach (var name in HousingData.InteractiveFurnishings.Where(n => n.Contains(_blanketFilter,
                                                                              StringComparison.OrdinalIgnoreCase)))
            {
                var enabled = configuration.FurnishingWhitelist.Contains(name);
                if (ImGui.Checkbox(name, ref enabled))
                {
                    if (enabled) configuration.FurnishingWhitelist.Add(name);
                    else configuration.FurnishingWhitelist.Remove(name);
                    configuration.Save();
                    RestoreAllTargetable();
                }
            }
        }

        ImGui.Spacing();
        if (ImGui.Button("Manage Arrows")) toggleWhitelistWindow();
    }
}
