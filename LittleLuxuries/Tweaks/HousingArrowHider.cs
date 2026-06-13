using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using LittleLuxuries.Housing;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace LittleLuxuries.Tweaks;

public class HousingArrowHider : Tweak, IDisposable
{
    public override string Name => "Hide Housing Arrows";
    public override string Description => "Hides the interaction arrows that appear in housing areas you own.";

    private readonly INamePlateGui namePlateGui;
    private readonly IClientState clientState;
    private readonly FurnishingScanner scanner;
    private readonly Action toggleWhitelistWindow;
    private readonly Configuration configuration;

    private ulong _currentHousingId;
    private Dictionary<uint, string>? _activeWhitelist;

    public ulong CurrentHousingId => _currentHousingId;
    public IReadOnlyDictionary<uint, string>? ActiveWhitelist => _activeWhitelist;

    private readonly HashSet<FurnishingId> _untargeted = new();
    private bool _zoneDirty;
    private string _blanketFilter = string.Empty;

    public HousingArrowHider(INamePlateGui namePlateGui, IClientState clientState, FurnishingScanner scanner, Action toggleWhitelistWindow, Configuration configuration)
    {
        this.namePlateGui = namePlateGui;
        this.clientState = clientState;
        this.scanner = scanner;
        this.toggleWhitelistWindow = toggleWhitelistWindow;
        this.configuration = configuration;

        namePlateGui.OnDataUpdate += OnDataUpdate;
        clientState.TerritoryChanged += OnTerritoryChanged;

        _zoneDirty = true;
    }

    public void Dispose()
    {
        namePlateGui.OnDataUpdate -= OnDataUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;
        RestoreAllTargetable();
    }

    public void AddToWhitelist(FurnishingId furnishingId, string name)
    {
        if (_currentHousingId == 0) return;

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

    private void OnTerritoryChanged(uint territory)
    {
        if (HousingData.TerritoryIds.Contains(territory)) _zoneDirty = true;
        else (_currentHousingId, _activeWhitelist, _zoneDirty) = (0, null, false);
        _untargeted.Clear();
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

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!configuration.HideHousingArrows) return;
        if (!HousingData.TerritoryIds.Contains(clientState.TerritoryType)) return;
        if (!configuration.PreventInteraction) RestoreAllTargetable();

        if (_zoneDirty)
        {
            var id = ReadIndoorHouseId();
            if (id != 0)
            {
                _currentHousingId = id;
                _activeWhitelist = configuration.UserWhitelist.GetValueOrDefault(id);
                _zoneDirty = false;
            }
        }

        foreach (var handler in handlers)
        {
            var gameObject = handler.GameObject;
            if (gameObject is null || gameObject.ObjectKind != ObjectKind.HousingEventObject) continue;

            var id = FurnishingId.From(gameObject);

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
