using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace LittleLuxuries.Tweaks;

public class HousingArrowHider : Tweak, IDisposable
{
    public override string Name => "Hide Housing Arrows";
    public override string Description => "Hides the interaction arrows that appear in housing areas you own.";

    private IAddonLifecycle addonLifecycle;
    private IClientState clientState;
    private Configuration configuration;

    private const int NamePlateCount = 50;
    private readonly HashSet<int> _hiddenIndices = new();

    private static readonly uint[] HouseTerritoryIds = {
        282, 283, 284, 384, 608,
        342, 343, 344, 385, 609,
        345, 346, 347, 386, 610,
        649, 650, 651, 652, 655,
        980, 981, 982, 983, 999,
        1249, 1250, 1251,
        1374, 1375, 1376,
    };

    public HousingArrowHider(IAddonLifecycle addonLifecycle, IClientState clientState, Configuration configuration)
    {
        this.addonLifecycle = addonLifecycle;
        this.clientState = clientState;
        this.configuration = configuration;
        clientState.TerritoryChanged += OnTerritoryChanged;

        if (HouseTerritoryIds.Contains(clientState.TerritoryType))
        {
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "NamePlate", OnNamePlatePostUpdate);
        }
    }

    public void Dispose()
    {
        clientState.TerritoryChanged -= OnTerritoryChanged;
        addonLifecycle?.UnregisterListener(AddonEvent.PostUpdate, "NamePlate", OnNamePlatePostUpdate);
    }

    private void OnTerritoryChanged(uint territory)
    {
        if (HouseTerritoryIds.Contains(territory))
        {
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "NamePlate", OnNamePlatePostUpdate);
        }
        else
        {
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "NamePlate", OnNamePlatePostUpdate);
            _hiddenIndices.Clear();
        }
    }

    private unsafe void OnNamePlatePostUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonNamePlate*)args.Addon.Address;

        for (var i = 0; i < NamePlateCount; i++)
        {
            var obj = addon->NamePlateObjectArray[i];
            var span = obj.NameText->NodeText.AsSpan();
            var isArrow = configuration.HideHousingArrows
                          && span.Length >= 3 && span[0] == 0xEE && span[1] == 0x80 && span[2] == 0xB5;

            if (isArrow)
            {
                obj.NameContainer->NodeFlags &= ~(NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents);
                _hiddenIndices.Add(i);
            }
            else if (_hiddenIndices.Remove(i))
            {
                obj.NameContainer->NodeFlags |= NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;
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
        }
    }
}
