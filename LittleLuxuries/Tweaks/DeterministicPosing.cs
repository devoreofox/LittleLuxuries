using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;
using LittleLuxuries.Services.Dpose;
using LittleLuxuries.UI;

namespace LittleLuxuries.Tweaks;

public class DeterministicPosing : Tweak, IDisposable
{
    public override string Name => "Deterministic Posing";
    public override string Description => "Extends the /cpose command to accept an index, allowing you to jump directly to a specific pose rather than cycling through them one at a time. For example, /cpose 3 immediately sets your third standing pose.";
    public override bool IsImplemented => true;

    private readonly CposeController controller;
    private readonly Configuration configuration;
    private readonly IChatGui chatGui;
    private Hook<ShellCommandModule.Delegates.ExecuteCommandInner> processChatInputHook;

    public unsafe DeterministicPosing(
        CposeController cpose, Configuration configuration, IChatGui chat, IGameInteropProvider interop)
    {
        this.controller = cpose;
        this.configuration = configuration;
        this.chatGui = chat;

        processChatInputHook = interop.HookFromAddress<ShellCommandModule.Delegates.ExecuteCommandInner>(
            ShellCommandModule.Addresses.ExecuteCommandInner.Value, Detour);
        processChatInputHook.Enable();

    }

    public void Dispose()
    {
        processChatInputHook.Dispose();
        controller.Dispose();
    }

    private unsafe void Detour(ShellCommandModule* shellCommandModule, Utf8String* message, UIModule* uiModule)
    {
        try
        {
            if (configuration.DeterministicPosing)
            {
                var parts = message->ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && parts[0].Equals("/cpose", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length == 1) processChatInputHook.Original(shellCommandModule, message, uiModule);
                    else HandleCpose(parts);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "cpose detour failed");
        }

        processChatInputHook.Original(shellCommandModule, message, uiModule);
    }

    private void HandleCpose(string[] parts)
    {
        switch (parts[1].ToLowerInvariant())
        {
            case "help":
                chatGui.Print("/cpose <index> - jump to a pose");
                chatGui.Print("/cpose list - show poses");
                chatGui.Print("/cpose - cycle");
                return;
            case "list":
                PrintList();
                return;
        }

        if (!byte.TryParse(parts[1], out var input))
        {
            chatGui.Print("Usage: /cpose <index> | list | help");
            return;
        }

        var type = controller.GetCurrentPoseType();
        if (type is null || !controller.IsPoseable())
        {
            chatGui.Print("/cpose <index> only works while standing, sitting, sitting on the ground, or dozing. ");
            return;
        }

        var max = controller.GetMaxPose(type.Value);
        var index = configuration.CposeOneBasedIndex ? input - 1 : input;
        if (index < 0 || index > max)
        {
            var low = configuration.CposeOneBasedIndex ? 1 : 0;
            var high = configuration.CposeOneBasedIndex ? max + 1 : max;
            chatGui.Print($"Pose {input} is out of range ({low}-{high} for {type}).");
            return;
        }
        controller.DriveTo((byte)index, configuration.CposeDelayMs);
    }

    private void PrintList()
    {
        var type = controller.GetCurrentPoseType();
        if (type is null || !controller.IsPoseable())
        {
            chatGui.Print("No indexed poses available right now.");
            return;
        }

        var max = controller.GetMaxPose(type.Value);
        var current = controller.GetCurrentPose() ?? 0;
        if (configuration.CposeOneBasedIndex)
            chatGui.Print($"{type}: 1-{max + 1} (current: {current + 1})");
        else
            chatGui.Print($"{type}: 0-{max} (current: {current})");
    }
    public override void DrawConfig()
    {
        var enabled = configuration.DeterministicPosing;
        if (ImGui.Checkbox("Deterministic Posing", ref enabled))
        {
            configuration.DeterministicPosing = enabled;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Enables /cpose <index> to jump directly to a pose.\nWhen off, /cpose behaves exactly like the game's built-in command.");

        var delay = configuration.CposeDelayMs;
        if (ImGui.SliderInt("Cycle delay (ms)", ref delay, 150, 1000))
        {
            configuration.CposeDelayMs = Math.Max(150, delay);
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Time between each step while cycling to your target pose.\nLower is faster; 150ms is the floor the game's pose timing allows.");

        var oneBased = configuration.CposeOneBasedIndex;
        if (ImGui.Checkbox("Number poses from 1", ref oneBased))
        {
            configuration.CposeOneBasedIndex = oneBased;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Count poses starting at 1 instead of 0, so /cpose 1 is your first pose.\nOff uses the game's native 0-based numbering.");

        ImGui.Spacing();
        ImGui.TextWrapped("/cpose <index> - jump straight to a specific pose.");
        ImGui.TextWrapped("/cpose list - show the available poses.");
        ImGui.TextWrapped("/cpose help - show usage.");
        ImGui.TextWrapped("Plain /cpose still cycles normally.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (controller.IsPoseable())
        {
            var current = controller.GetCurrentPose() ?? 0;
            var display = configuration.CposeOneBasedIndex ? current + 1 : current;
            ImGui.Text($"Pose type: {controller.GetCurrentPoseType()}");
            ImGui.Text($"Pose number: {display}");
        }
        else
        {
            ImGui.Text("Pose type: None");
            ImGui.Text("Pose number: None");
        }
    }
}
