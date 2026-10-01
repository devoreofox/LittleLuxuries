using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using ECommons.ExcelServices;

namespace LittleLuxuries.UI.EstateLabels;

public sealed class WorldPicker
{
    private readonly IPlayerState playerState;

    private string filter = "";
    private (string Region, (string DataCenter, (uint Id, string Name)[] Worlds)[] DataCenters)[]? tree;

    public WorldPicker(IPlayerState playerState)
    {
        this.playerState = playerState;
    }

    public void Draw(string id, ref uint world)
    {
        var preview = world == 0 ? "Select a world" : ExcelWorldHelper.GetName(world);
        if (!ImGui.BeginCombo(id, preview, ImGuiComboFlags.HeightLarge)) return;

        var appearing = ImGui.IsWindowAppearing();
        if (appearing)
        {
            filter = "";
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##worldfilter", "Search...", ref filter, 32);

        tree ??= Enum.GetValues<ExcelWorldHelper.Region>()
                     .Select(r => (r.ToString(), ExcelWorldHelper.GetDataCenters(r, checkForPublicWorlds: true).Select(dc => (dc.Name.ExtractText(), ExcelWorldHelper.GetPublicWorlds(dc.RowId).Select(w => (w.RowId, w.Name.ExtractText())).OrderBy(w => w.Item2).ToArray())).ToArray())).ToArray();

        var focus = world != 0 ? world : playerState.HomeWorld.RowId;
        var filtering = filter.Length > 0;
        var search = filter;

        foreach (var (region, dataCenters) in tree)
        {
            var visible = dataCenters
                          .Select(dc => (dc.DataCenter, Worlds: dc.Worlds.Where(w => w.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray()))
                          .Where(dc => dc.Worlds.Length > 0)
                          .ToArray();
            if (visible.Length == 0) continue;

            if (filtering) ImGui.SetNextItemOpen(true);
            else if (appearing) ImGui.SetNextItemOpen(visible.Any(dc => dc.Worlds.Any(w => w.Id == focus)));
            if (!ImGui.TreeNode(region)) continue;

            foreach (var (dataCenter, worlds) in visible)
            {
                if (filtering) ImGui.SetNextItemOpen(true);
                else if (appearing) ImGui.SetNextItemOpen(worlds.Any(w => w.Id == focus));
                if (!ImGui.TreeNode(dataCenter)) continue;

                foreach (var (worldId, name) in worlds)
                {
                    ImGui.Bullet();
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{name}##{worldId}", worldId == world)) world = worldId;
                    if (appearing && worldId == focus) ImGui.SetScrollHereY();
                }
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }
        ImGui.EndCombo();
    }
}
