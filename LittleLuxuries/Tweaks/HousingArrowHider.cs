using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;

namespace LittleLuxuries.Tweaks;

public class HousingArrowHider : Tweak, IDisposable
{
    public override string Name => "Hide Housing Arrows";
    public override string Description => "Hides the interaction arrows that appear in housing areas you own.";

    private readonly INamePlateGui namePlateGui;
    private readonly IClientState clientState;
    private readonly Configuration configuration;

    private static readonly uint[] HouseTerritoryIds = {
        282, 283, 284, 384, 608,
        342, 343, 344, 385, 609,
        345, 346, 347, 386, 610,
        649, 650, 651, 652, 655,
        980, 981, 982, 983, 999,
        1249, 1250, 1251,
        1374, 1375, 1376,
    };

    public HousingArrowHider(INamePlateGui namePlateGui, IClientState clientState, Configuration configuration)
    {
        this.namePlateGui = namePlateGui;
        this.clientState = clientState;
        this.configuration = configuration;
        namePlateGui.OnDataUpdate += OnDataUpdate;
    }

    public void Dispose()
    {
        namePlateGui.OnDataUpdate -= OnDataUpdate;
    }

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!configuration.HideHousingArrows) return;
        if (!HouseTerritoryIds.Contains(clientState.TerritoryType)) return;

        foreach (var handler in handlers)
        {
            var span = handler.GetFieldAsSpan(NamePlateStringField.Name);
            if (span.Length >= 3 && span[0] == 0xEE && span[1] == 0x80 && span[2] == 0xB5)
            {
                handler.VisibilityFlags = 0;
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
