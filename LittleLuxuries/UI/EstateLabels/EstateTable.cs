using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using LittleLuxuries.Models.Housing;

namespace LittleLuxuries.UI.EstateLabels;

public sealed class EstateTable
{
    private readonly Configuration configuration;

    public EstateTable(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Draw(ulong cid, EstateLabelCharacter character)
    {
        ImGuiUtil.Section("Your estates");
        if (character.Estates.Count == 0)
        {
            ImGui.TextDisabled("Open the Teleport menu once to load your estates.");
            return;
        }

        if (!ImGui.BeginTable($"##estates{cid}", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit)) return;
        ImGui.TableSetupColumn("##order");
        ImGui.TableSetupColumn("##location");
        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##forget");

        var estates = character.Estates.OrderBy(e => e.Value.Order).ToList();
        for (var j = 0; j < estates.Count; j++)
        {
            var (houseId, estate) = estates[j];
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.BeginDisabled(j == 0);
            if (ImGui.ArrowButton($"##up{houseId}", ImGuiDir.Up)) Move(estates, j, -1);
            ImGui.EndDisabled();
            ImGui.SameLine(0, 2);
            ImGui.BeginDisabled(j == estates.Count - 1);
            if (ImGui.ArrowButton($"##down{houseId}", ImGuiDir.Down)) Move(estates, j, +1);
            ImGui.EndDisabled();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(estate.Location);

            ImGui.TableNextColumn();
            var label = estate.Label;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint($"##label{houseId}", "No label", ref label, 64)) estate.Label = label;
            if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton($"##forget{houseId}", FontAwesomeIcon.Trash))
            {
                character.Estates.Remove(houseId);
                configuration.Save();
            }
            ImGuiUtil.Tooltip("Forget this estate and its label. Estates you still have come back next time Teleport opens.");
        }
        ImGui.EndTable();
    }

    private void Move(List<KeyValuePair<ulong, EstateLabel>> estates, int j, int dir)
    {
        (estates[j], estates[j + dir]) = (estates[j + dir], estates[j]);
        for (var k = 0; k < estates.Count; k++) estates[k].Value.Order = k;
        configuration.Save();
    }
}
