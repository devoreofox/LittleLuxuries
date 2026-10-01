using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using LittleLuxuries.Models.Housing;
using LittleLuxuries.Services.Housing;

namespace LittleLuxuries.UI.EstateLabels;

public sealed class BookmarkTable
{
    private readonly Configuration configuration;
    private readonly EstateAddresses addresses;

    public BookmarkTable(Configuration configuration, EstateAddresses addresses)
    {
        this.configuration = configuration;
        this.addresses = addresses;
    }

    public void Draw(ulong cid, EstateLabelCharacter character)
    {
        ImGuiUtil.Section("Bookmarks");
        if (character.Bookmarks.Count == 0)
        {
            ImGui.TextDisabled("No bookmarks yet. They appear at the end of Residential Areas.");
            return;
        }

        if (!ImGui.BeginTable($"##bookmarks{cid}", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit)) return;
        ImGui.TableSetupColumn("##order");
        ImGui.TableSetupColumn("##location");
        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##remove");

        (int From, int To)? move = null;

        for (var j = 0; j < character.Bookmarks.Count; j++)
        {
            var b = character.Bookmarks[j];
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.BeginDisabled(j == 0);
            if (ImGui.ArrowButton($"##bmup{cid}-{j}", ImGuiDir.Up)) move = (j, j - 1);
            ImGui.EndDisabled();
            ImGui.SameLine(0, 2);
            ImGui.BeginDisabled(j == character.Bookmarks.Count - 1);
            if (ImGui.ArrowButton($"##bmdown{cid}-{j}", ImGuiDir.Down)) move = (j, j + 1);
            ImGui.EndDisabled();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(addresses.FormatBookmark(b));

            ImGui.TableNextColumn();
            var label = b.Label;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText($"##bmlabel{cid}-{j}", ref label, 64)) b.Label = label;
            if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton($"##bmremove{cid}-{j}", FontAwesomeIcon.Trash))
            {
                character.Bookmarks.RemoveAt(j);
                configuration.Save();
                break;
            }
            ImGuiUtil.Tooltip("Remove this bookmark.");
        }
        ImGui.EndTable();

        if (move is var (from, to))
        {
            (character.Bookmarks[from], character.Bookmarks[to]) = (character.Bookmarks[to], character.Bookmarks[from]);
            configuration.Save();
        }
    }
}
