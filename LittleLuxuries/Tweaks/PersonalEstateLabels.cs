using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using LittleLuxuries.Services.Housing;
using LittleLuxuries.UI;
using LittleLuxuries.UI.EstateLabels;

namespace LittleLuxuries.Tweaks;

public class PersonalEstateLabels : Tweak, IDisposable
{
    private readonly Configuration configuration;
    private readonly EstateRegistry registry;
    private readonly TeleportListEditor teleportList;
    private readonly TravelSettings travelSettings;
    private readonly EstateTable estateTable;
    private readonly BookmarkTable bookmarkTable;
    private readonly BookmarkForm bookmarkForm;

    public PersonalEstateLabels(Configuration configuration, EstateRegistry registry, TeleportListEditor teleportList, TravelSettings travelSettings,
                                EstateTable estateTable, BookmarkTable bookmarkTable, BookmarkForm bookmarkForm)
    {
        this.configuration = configuration;
        this.registry = registry;
        this.teleportList = teleportList;
        this.travelSettings = travelSettings;
        this.estateTable = estateTable;
        this.bookmarkTable = bookmarkTable;
        this.bookmarkForm = bookmarkForm;
    }

    public override string Name => "Personal Estate Labels";
    public override string Description => "Give your personal estate, shared estates and apartment custom names in the Teleport menu, optionally with ward and plot numbers, and reorder them so your favourite place sits at the top. " +
                                          "Bookmark friends' houses on any world and click them in the Teleport menu to travel there: all the way to the plot with Lifestream, or as close as a teleport gets you without it.";
    public override bool IsImplemented => true;

    public void Dispose()
    {
        teleportList.Dispose();
        registry.Dispose();
    }

    public override void DrawConfig()
    {
        var enabled = configuration.PersonalEstateLabels;
        var labelFirst = configuration.EstateLabelsLabelFirst;
        var showWorld = configuration.EstateLabelsWorld;
        var showDistrict = configuration.EstateLabelsDistrict;
        var showWard = configuration.EstateLabelsWard;
        var showPlot = configuration.EstateLabelsPlot;
        var bookmarkWorld = configuration.EstateBookmarkWorld;
        var bookmarkDistrict = configuration.EstateBookmarkDistrict;
        var bookmarkWard = configuration.EstateBookmarkWard;
        var bookmarkPlot = configuration.EstateBookmarkPlot;

        if (ImGui.Checkbox("Show custom estate labels in the Teleport menu", ref enabled))
        {
            configuration.PersonalEstateLabels = enabled;
            configuration.Save();

            if (enabled) registry.Track();
        }

        if (ImGui.Checkbox("Show label before location", ref labelFirst))
        {
            configuration.EstateLabelsLabelFirst = labelFirst;
            configuration.Save();
        }

        var toggleColumn = ImGui.CalcTextSize("Bookmark labels").X + ImGui.GetStyle().ItemSpacing.X * 2;

        if (PartToggles("Default labels", "estate", toggleColumn, ref showWorld, ref showDistrict, ref showWard, ref showPlot))
        {
            configuration.EstateLabelsWorld = showWorld;
            configuration.EstateLabelsDistrict = showDistrict;
            configuration.EstateLabelsWard = showWard;
            configuration.EstateLabelsPlot = showPlot;
            configuration.Save();
            registry.Refresh();
        }

        if (PartToggles("Bookmark labels", "bookmark", toggleColumn, ref bookmarkWorld, ref bookmarkDistrict, ref bookmarkWard, ref bookmarkPlot))
        {
            configuration.EstateBookmarkWorld = bookmarkWorld;
            configuration.EstateBookmarkDistrict = bookmarkDistrict;
            configuration.EstateBookmarkWard = bookmarkWard;
            configuration.EstateBookmarkPlot = bookmarkPlot;
            configuration.Save();
        }

        var split = configuration.EstateLabelsSplitHeadings;
        if (ImGui.Checkbox("Split Residential Areas into two headings", ref split))
        {
            configuration.EstateLabelsSplitHeadings = split;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Your estates on this world go under the first heading. Bookmarks, and your estates on other worlds, go under the second.");

        if (split)
        {
            ImGui.Indent();
            HeadingInput("First heading##directHeading", TeleportListEditor.DefaultDirectHeading, configuration.EstateLabelsDirectHeading,
                         h => configuration.EstateLabelsDirectHeading = h);
            HeadingInput("Second heading##bookmarkHeading", TeleportListEditor.DefaultBookmarkHeading, configuration.EstateLabelsBookmarkHeading,
                         h => configuration.EstateLabelsBookmarkHeading = h);
            ImGui.Unindent();
        }

        travelSettings.Draw();

        ulong? forget = null;
        foreach (var (cid, character) in configuration.EstateLabels)
        {
            if (!ImGui.CollapsingHeader($"{character.Name}##{cid}")) continue;

            ImGui.Indent();
            estateTable.Draw(cid, character);
            bookmarkTable.Draw(cid, character);
            bookmarkForm.Draw(cid, character);

            ImGui.Spacing();
            ImGui.PushID($"forget{cid}");
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "Forget character") && ImGui.GetIO().KeyCtrl) forget = cid;
            ImGuiUtil.Tooltip("Hold Ctrl and click to forget this character, along with all its labels and bookmarks.");
            ImGui.PopID();
            ImGui.Unindent();
        }

        if (forget is not { } forgotten) return;

        var wasCurrent = registry.Current == configuration.EstateLabels[forgotten];
        configuration.EstateLabels.Remove(forgotten);
        configuration.Save();
        if (wasCurrent) registry.Track();
    }

    private void HeadingInput(string label, string fallback, string heading, Action<string> set)
    {
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint(label, fallback, ref heading, 64)) set(heading);
        if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();
    }

    private static bool PartToggles(string label, string id, float column, ref bool world, ref bool district, ref bool ward, ref bool plot)
    {
        ImGui.TextUnformatted(label);
        ImGui.SameLine(column);
        var changed = ImGui.Checkbox($"World##{id}World", ref world);
        ImGui.SameLine();
        changed |= ImGui.Checkbox($"District##{id}District", ref district);
        ImGui.SameLine();
        changed |= ImGui.Checkbox($"Ward##{id}Ward", ref ward);
        ImGui.SameLine();
        changed |= ImGui.Checkbox($"Plot##{id}Plot", ref plot);
        ImGuiUtil.Tooltip("Plot number, or apartment number for apartments.");
        return changed;
    }
}
