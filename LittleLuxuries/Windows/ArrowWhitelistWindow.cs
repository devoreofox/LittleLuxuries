using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using LittleLuxuries.Housing;
using LittleLuxuries.Tweaks;

namespace LittleLuxuries.Windows;

public class ArrowWhitelistWindow : Window
{
    private readonly HousingArrowHider tweak;
    private readonly FurnishingScanner scanner;
    private List<Furnishing> _furnishings = new();

    private string _filter = string.Empty;

    public ArrowWhitelistWindow(HousingArrowHider tweak, FurnishingScanner scanner) : base(
        "Arrow Whitelist##ArrowWhitelist")
    {
        this.tweak = tweak;
        this.scanner = scanner;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        if (tweak.CurrentHousingId == 0)
        {
            ImGui.TextDisabled("Enter a housing zone to manage arrows.");
            return;
        }
        if (!tweak.CanManageCurrentHouse)
        {
            ImGui.TextDisabled("You don't have permission to manage arrows in this house.");
            return;
        }

        _furnishings = scanner.Enumerate().ToList();

        var active = tweak.ActiveWhitelist;
        var lineHeight= ImGui.GetTextLineHeightWithSpacing();
        var buttonWidth= ImGui.CalcTextSize("Whitelist").X + ImGui.GetStyle().FramePadding.X * 2 + 10f;

        var whitelistedCount = active?.Count ?? 0;
        var whitelistedHeight= lineHeight * 3 + Math.Max(lineHeight, whitelistedCount * lineHeight);
        var furnishingsHeight= Math.Max(100f, ImGui.GetContentRegionAvail().Y - whitelistedHeight - lineHeight * 2);

        ImGui.TextColored(new Vector4(0.7f, 0.5f, 1.0f, 1.0f), "Furnishings");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##furnishingFilter", "Search...", ref _filter, 100);
        ImGui.Spacing();

        if (ImGui.BeginChild("##furnishings", new Vector2(0, furnishingsHeight)))
        {
            var any = false;
            foreach (var furnishing in _furnishings.Where(f => f.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)))
            {
                any = true;
                var icon = tweak.IsHighlighted(furnishing.Id) ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye;

                ImGui.PushID((int)furnishing.Id.Value);
                if (ImGuiComponents.IconButton(icon)) tweak.ToggleHighlight(furnishing.Id);
                ImGui.SameLine();
                ImGui.Text(furnishing.Name);
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonWidth);

                if (active?.ContainsKey(furnishing.Id.Value) ?? false)
                {
                    if (ImGui.Button("Remove", new Vector2(buttonWidth, 0))) tweak.RemoveFromWhitelist(furnishing.Id);
                }
                else
                {
                    if (ImGui.Button("Whitelist", new Vector2(buttonWidth, 0))) tweak.AddToWhitelist(furnishing.Id, furnishing.Name);
                }
                ImGui.PopID();
            }
            if (!any) ImGui.TextDisabled("No furnishings found.");
        }
        ImGui.EndChild();

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.5f, 1.0f, 1.0f), "Whitelisted");
        ImGui.Separator();
        ImGui.Spacing();

        if (active is null || active.Count == 0)
        {
            ImGui.TextDisabled("No specific furnishings whitelisted for this house.");
        }
        else
        {
            foreach (var (idValue, name) in active.ToList())
            {
                ImGui.PushID($"wl{idValue}");
                ImGui.Text(name);
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonWidth);
                if (ImGui.Button("Remove", new Vector2(buttonWidth, 0)))
                    tweak.RemoveFromWhitelist(new FurnishingId(idValue));
                ImGui.PopID();
            }
        }
    }
}
