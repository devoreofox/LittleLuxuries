using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using LittleLuxuries.Services.Housing;
using LittleLuxuries.UI;

namespace LittleLuxuries.Tweaks;

public sealed class EstateKey : Tweak, IDisposable
{
    private readonly EstateAccessController controller;
    private readonly Configuration configuration;
    private readonly ICommandManager commands;
    private readonly IChatGui chatGui;

    public EstateKey(EstateAccessController controller, Configuration configuration, ICommandManager commands, IChatGui chatGui)
    {
        this.controller = controller;
        this.configuration = configuration;
        this.commands = commands;
        this.chatGui = chatGui;

        commands.AddHandler("/lock", new CommandInfo(OnAccessCommand)
            { HelpMessage = "Lock your estate's guest access. Optional: personal | apartment | chambers | fc" });
        commands.AddHandler("/unlock", new CommandInfo(OnAccessCommand)
            { HelpMessage = "Unlock your estate's guest access. Optional: personal | apartment | chambers | fc" });
        commands.AddHandler("/estatetp", new CommandInfo(OnTeleportCommand)
            { HelpMessage = "Toggle estate teleport. Usage: /estatetp on|off [target]" });
    }

    public override string Name => "Estate Lock";
    public override string Description =>
        "Adds /lock and /unlock to toggle your estate's guest access without opening the housing menus, plus /estatetp to control teleport permission. Works from anywhere on your home world.";
    public override bool IsImplemented => true;

    public void Dispose()
    {
        commands.RemoveHandler("/lock");
        commands.RemoveHandler("/unlock");
        commands.RemoveHandler("/estatetp");
        controller.Dispose();
    }

    private void OnAccessCommand(string command, string args)
    {
        if (!configuration.EstateKey) return;

        if (!TryParseEstate(args, out var estate))
        {
            chatGui.PrintError("Usage: target must be personal | apartment | chambers | fc, or leave it blank.");
            return;
        }

        var unlock = command.Equals("/unlock", StringComparison.OrdinalIgnoreCase);
        if (!controller.TrySetGuestAccess(estate, unlock, null))
            chatGui.PrintError("Couldn't change estate access - you must be on your home world, out of instanced content, and own that estate.");
    }

    private void OnTeleportCommand(string command, string args)
    {
        if (!configuration.EstateKey) return;

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool? on = null;
        if (parts.Length > 0)
        {
            if (parts[0].Equals("on", StringComparison.OrdinalIgnoreCase)) on = true;
            else if (parts[0].Equals("off", StringComparison.OrdinalIgnoreCase)) on = false;
        }
        if (on is null)
        {
            chatGui.PrintError("Usage: /estatetp on|off [target]");
            return;
        }

        var targetArg = parts.Length > 1 ? parts[1] : "";
        if (!TryParseEstate(targetArg, out var estate))
        {
            chatGui.PrintError("Usage: target must be personal | apartment | chambers | fc, or leave it blank.");
            return;
        }

        if (!controller.TrySetGuestAccess(estate, null, on))
            chatGui.PrintError("Couldn't change estate teleport - you must be on your home world, out of instanced content, and own that estate.");
    }

    private static bool TryParseEstate(string arg, out EstateType? estate)
    {
        estate = null;
        switch (arg.Trim().ToLowerInvariant())
        {
            case "": return true;                                                       // cascade to first owned
            case "p": case "personal":  estate = EstateType.PersonalEstate;   return true;
            case "a": case "apartment": estate = EstateType.ApartmentRoom;    return true;
            case "c": case "chambers":  estate = EstateType.PersonalChambers; return true;
            case "f": case "fc": case "freecompany": estate = EstateType.FreeCompanyEstate; return true;
            default: return false;
        }
    }

    public override void DrawConfig()
    {
        var enabled = configuration.EstateKey;
        if (ImGui.Checkbox("Estate Lock commands", ref enabled))
        {
            configuration.EstateKey = enabled;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Enables /lock, /unlock, and /estatetp. When off, the commands do nothing.");

        ImGui.Spacing();
        ImGui.TextWrapped("/lock [target] - turn off guest access (keeps the teleport setting).");
        ImGui.TextWrapped("/unlock [target] - turn on guest access (keeps the teleport setting).");
        ImGui.TextWrapped("/estatetp on|off [target] - turn estate teleport on or off.");
        ImGui.Spacing();
        ImGui.TextWrapped("target: personal | apartment | chambers | fc. Leave blank to use your first owned estate.");
        ImGui.TextWrapped("Only works on your home world and outside instanced content.");
    }
}
