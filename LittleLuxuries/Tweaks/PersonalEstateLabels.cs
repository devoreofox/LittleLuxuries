using System;
using Dalamud.Bindings.ImGui;
using LittleLuxuries.Services.Housing;
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
        var labelNumbers = configuration.EstateLabelsNumbers;

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
        ImGui.SameLine();
        if (ImGui.Checkbox("Show ward and plot numbers", ref labelNumbers))
        {
            configuration.EstateLabelsNumbers = labelNumbers;
            configuration.Save();
            registry.Refresh();
        }

        travelSettings.Draw();

        foreach (var (cid, character) in configuration.EstateLabels)
        {
            if (!ImGui.CollapsingHeader($"{character.Name}##{cid}")) continue;

            ImGui.Indent();
            estateTable.Draw(cid, character);
            bookmarkTable.Draw(cid, character);
            bookmarkForm.Draw(cid, character);
            ImGui.Unindent();
        }
    }
}
