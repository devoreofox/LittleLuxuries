using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using LittleLuxuries.Housing;

namespace LittleLuxuries.Tweaks;

public class HousingArrowHider : Tweak, IDisposable
{
    public override string Name => "Hide Housing Arrows";
    public override string Description => "Hides the interaction arrows that appear in housing areas you own.";

    private readonly INamePlateGui namePlateGui;
    private readonly IClientState clientState;
    private readonly Configuration configuration;

    private ulong _currentHousingId;
    private Dictionary<uint, string>? _activeWhitelist;
    private bool _zoneDirty;
    private string _blanketFilter = string.Empty;

    public HousingArrowHider(INamePlateGui namePlateGui, IClientState clientState, Configuration configuration)
    {
        this.namePlateGui = namePlateGui;
        this.clientState = clientState;
        this.configuration = configuration;

        namePlateGui.OnDataUpdate += OnDataUpdate;
        clientState.TerritoryChanged += OnTerritoryChanged;

        _zoneDirty = true;
    }

    public void Dispose()
    {
        namePlateGui.OnDataUpdate -= OnDataUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void OnTerritoryChanged(uint territory)
    {
        if (HousingData.TerritoryIds.Contains(territory)) _zoneDirty = true;
        else (_currentHousingId, _activeWhitelist, _zoneDirty) = (0, null, false);
    }

    private unsafe ulong ReadIndoorHouseId()
    {
        var mgr = HousingManager.Instance();
        return mgr != null ? mgr->GetCurrentIndoorHouseId().Id : 0;
    }

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!configuration.HideHousingArrows) return;
        if (!HousingData.TerritoryIds.Contains(clientState.TerritoryType)) return;

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

            if (configuration.FurnishingWhitelist.Contains(gameObject.Name.TextValue)) continue;

            var id = FurnishingId.From(gameObject);
            if (_activeWhitelist?.ContainsKey(id.Value) == true) continue;

            handler.VisibilityFlags = 0;
        }
    }

    public override void DrawConfig()
    {
        var hideArrows = configuration.HideHousingArrows;
        if (ImGui.Checkbox("Hide Housing Arrows", ref hideArrows))
        {
            configuration.HideHousingArrows = hideArrows;
            configuration.Save();
        }

        if (!configuration.HideHousingArrows) return;

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
                }
            }
        }
    }
}
