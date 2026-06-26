using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using LittleLuxuries.Tweaks;

namespace LittleLuxuries.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string _filter = string.Empty;
    private Tweak? _selectedTweak;

    public MainWindow(Plugin plugin, Action openChangelog) : base("Little Luxuries###Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Scroll,
            Click = _ => openChangelog(),
            ShowTooltip = () => ImGui.SetTooltip("Changelog")
        });

        this.plugin = plugin;
        _selectedTweak = plugin.Tweaks.FirstOrDefault();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var leftWidth = new Vector2(225 * scale, 0);

        ImGui.BeginChild("###tweakSelector", leftWidth, true);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###filter", "Search...", ref _filter, 100);
        ImGui.Separator();

        var filtered = plugin.Tweaks.Where(t => string.IsNullOrWhiteSpace(_filter) || t.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var tweak in filtered)
        {
            var isNew = !plugin.Configuration.NewTweaks.Contains(tweak.Name);

            if (ImGui.Selectable(tweak.Name, _selectedTweak == tweak))
            {
                _selectedTweak = tweak;
                if (isNew)
                {
                    plugin.Configuration.NewTweaks.Add(tweak.Name);
                    plugin.Configuration.Save();
                }
            }

            if (isNew)
            {
                const string badge = "New!";
                var width = ImGui.CalcTextSize(badge).X;
                ImGui.SameLine(ImGui.GetContentRegionMax().X - width - ImGui.GetStyle().ItemSpacing.X);
                ImGui.TextColored(new Vector4(0.7f, 0.5f, 1.0f, 1.0f), badge);
            }
        }

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("###tweakPanel", new Vector2(0, 0), true);

        if (_selectedTweak != null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.5f, 1.0f, 1.0f), _selectedTweak.Name);
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextWrapped(_selectedTweak.Description);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (!_selectedTweak.IsImplemented)
            {
                var text = "Coming soon.";
                var textSize = ImGui.CalcTextSize(text);
                var region = ImGui.GetContentRegionAvail();
                ImGui.SetCursorPos(new Vector2((region.X - textSize.X) / 2, (region.Y - textSize.Y) / 2));
                ImGui.TextDisabled(text);
            }
            else
            {
                _selectedTweak.DrawConfig();
            }
        }
        else
        {
            var text = "No tweak selected.";
            var textSize = ImGui.CalcTextSize(text);
            var region = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2((region.X - textSize.X) / 2, (region.Y - textSize.Y) / 2));
            ImGui.TextDisabled(text);
        }

        ImGui.EndChild();
    }
}
