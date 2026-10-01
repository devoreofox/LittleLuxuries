using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using LittleLuxuries.Services.Housing;
using Lumina.Excel.Sheets;

namespace LittleLuxuries.UI.EstateLabels;

public sealed class TravelSettings
{
    private static readonly Vector4 Connected = new(0.45f, 0.85f, 0.45f, 1f);
    private static readonly Vector4 Disconnected = new(0.9f, 0.45f, 0.45f, 1f);
    private static readonly int[] TravelCities = { 8, 9, 2 };

    private readonly Configuration configuration;
    private readonly IDataManager dataManager;
    private readonly BookmarkTravel travel;

    public TravelSettings(Configuration configuration, IDataManager dataManager, BookmarkTravel travel)
    {
        this.configuration = configuration;
        this.dataManager = dataManager;
        this.travel = travel;
    }

    public void Draw()
    {
        ImGuiUtil.Section("Bookmark travel");

        var lifestream = travel.LifestreamAvailable();
        ImGui.TextUnformatted("Lifestream");
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(lifestream ? Connected : Disconnected, (lifestream ? FontAwesomeIcon.Check : FontAwesomeIcon.Times).ToIconString());
        ImGui.PopFont();
        ImGuiUtil.Tooltip(lifestream
            ? "Clicking a bookmark lets Lifestream take you all the way to the plot."
            : "Lifestream isn't installed or enabled. Clicking a bookmark teleports you as close as it can instead.");

        var aetherytes = dataManager.GetExcelSheet<Aetheryte>();
        var travelNames = TravelCities.Select(a => aetherytes.GetRow((uint)a).PlaceName.Value.Name.ExtractText()).ToArray();
        var travelIndex = Array.IndexOf(TravelCities, configuration.EstateBookmarkTravelCity);

        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("World visit from", ref travelIndex, travelNames))
        {
            configuration.EstateBookmarkTravelCity = TravelCities[travelIndex];
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Without Lifestream, clicking a bookmark on another world teleports you here so you can world visit.");
    }
}
