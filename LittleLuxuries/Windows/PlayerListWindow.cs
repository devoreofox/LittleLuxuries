using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ECommons.ExcelServices;
using LittleLuxuries.Tweaks;
using LittleLuxuries.UI;
using LittleLuxuries.UI.EstateLabels;

namespace LittleLuxuries.Windows;

public class PlayerListWindow : Window
{
    private readonly PlayerMapIcons tweak;
    private readonly Configuration configuration;
    private readonly WorldPicker worldPicker;

    private string _name = string.Empty;
    private uint _world;

    public PlayerListWindow(PlayerMapIcons tweak, Configuration configuration, WorldPicker worldPicker) : base("Map Players##PlayerList")
    {
        this.tweak = tweak;
        this.configuration = configuration;
        this.worldPicker = worldPicker;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGui.InputTextWithHint("##playerName", "Player Name", ref _name, 32);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        worldPicker.Draw("##playerWorld", ref _world);

        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_name) || _world == 0);
        if (ImGui.Button("Whitelist")) Add(true);
        ImGui.SameLine();
        if (ImGui.Button("Blacklist")) Add(false);
        ImGui.EndDisabled();

        DrawList("Whitelist", configuration.PlayerMapIconsWhitelist, true);
        DrawList("Blacklist", configuration.PlayerMapIconsBlacklist, false);
    }

    private void Add(bool whitelist)
    {
        tweak.AddToList($"{_name.Trim()}@{ExcelWorldHelper.GetName(_world)}", whitelist);
        _name = string.Empty;
    }

    private void DrawList(string title, HashSet<string> list, bool colored)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.5f, 1.0f, 1.0f), title);
        ImGui.Separator();
        ImGui.Spacing();

        if (list.Count == 0)
        {
            ImGui.TextDisabled("Nobody here yet.");
            return;
        }

        var buttonWidth = ImGui.CalcTextSize("Remove").X + ImGui.GetStyle().FramePadding.X * 2 + 10f;
        foreach (var key in list.ToList())
        {
            ImGui.PushID(title + key);
            if (colored) DrawColor(key);
            ImGui.Text(key);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonWidth);
            if (ImGui.Button("Remove", new Vector2(buttonWidth, 0))) tweak.RemoveFromLists(key);
            ImGui.PopID();
        }
    }

    private void DrawColor(string key)
    {
        var color = tweak.WhitelistColor(key);
        if (ImGui.ColorEdit3("##color", ref color, ImGuiColorEditFlags.NoInputs)) tweak.SetCustomColor(key, color);
        if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();
        ImGuiUtil.Tooltip("Give this player their own marker colour. Overrides the Whitelisted Players colour.");
        ImGui.SameLine();

        if (!tweak.HasCustomColor(key)) return;
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Undo)) tweak.ClearCustomColor(key);
        ImGuiUtil.Tooltip("Go back to the Whitelisted Players colour.");
        ImGui.SameLine();
    }
}
