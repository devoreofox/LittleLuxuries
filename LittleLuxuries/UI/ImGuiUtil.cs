using Dalamud.Bindings.ImGui;

namespace LittleLuxuries.UI;

public static class ImGuiUtil
{
    public static void Tooltip(string text)
    {
        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
