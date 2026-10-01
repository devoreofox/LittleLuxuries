using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.UIHelpers.AddonMasterImplementations;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LittleLuxuries.UI;
using Lumina.Excel.Sheets;

namespace LittleLuxuries.Tweaks;

public sealed unsafe class QuickCommands : Tweak, IDisposable
{
    private const uint ApartmentEntranceDataId = 2007402;

    private readonly ICommandManager commands;
    private readonly IObjectTable objects;
    private readonly IClientState clientState;
    private readonly IChatGui chat;
    private readonly IFramework framework;
    private readonly Configuration configuration;

    private bool entering;
    private int enterFrames;
    private int? targetRoom;

    public QuickCommands(ICommandManager commands, IObjectTable objects, IClientState clientState,
                         IChatGui chat, IFramework framework, Configuration configuration)
    {
        this.commands = commands;
        this.objects = objects;
        this.clientState = clientState;
        this.chat = chat;
        this.framework = framework;
        this.configuration = configuration;

        commands.AddHandler("/targetnearest", new CommandInfo(OnTargetNearest) { HelpMessage = "Target the nearest interactable object (housing door, entrance, etc.)." });
        commands.AddHandler("/acceptduty",    new CommandInfo(OnAcceptDuty)    { HelpMessage = "Press Commence on the Duty Ready popup." });
        commands.AddHandler("/enterhouse",    new CommandInfo(OnEnterHouse)    { HelpMessage = "Enter the house or apartment you're standing in front of." });
        commands.AddHandler("/itemaction",    new CommandInfo(OnItemAction)    { HelpMessage = "Use an inventory item by its name (potions, food, prisms etc)." });

        framework.Update += OnFrameworkUpdate;
    }

    public override string Name => "Quick Commands";
    public override string Description => "Adds /targetnearest, /acceptduty, /enterhouse, and /itemaction for one-press interactions.";
    public override bool IsImplemented => true;

    public void Dispose()
    {
        commands.RemoveHandler("/targetnearest");
        commands.RemoveHandler("/acceptduty");
        commands.RemoveHandler("/enterhouse");
        commands.RemoveHandler("/itemaction");
        framework.Update -= OnFrameworkUpdate;
    }

    private static bool Ready(string name, out AtkUnitBase* addon)
        => GenericHelpers.TryGetAddonByName(name, out addon) && GenericHelpers.IsAddonReady(addon);

    private IGameObject? Nearest(Func<IGameObject, bool> match)
    {
        var me = clientState.LocalPlayer;
        if (me is null) return null;
        return objects.Where(match).OrderBy(o => Vector3.Distance(o.Position, me.Position)).FirstOrDefault();
    }

    private void OnTargetNearest(string command, string args)
    {
        if (!configuration.QuickCommands) return;

        var obj = Nearest(o => o.IsTargetable && o.ObjectKind == ObjectKind.EventObj);
        if (obj is null) { chat.PrintError("[Little Luxuries] No interactable object nearby."); return; }

        Svc.Targets.Target = obj;
    }

    private void OnAcceptDuty(string command, string args)
    {
        if (!configuration.QuickCommands) return;

        if (Ready("ContentsFinderConfirm", out var a))
            new AddonMaster.ContentsFinderConfirm((nint)a).Commence();
        else
            chat.PrintError("[Little Luxuries] No duty ready to accept.");
    }

    private void OnEnterHouse(string command, string args)
    {
        if (!configuration.QuickCommands) return;

        targetRoom = null;
        var arg = args.Trim();
        if (arg.Length > 0)
        {
            if (!int.TryParse(arg, out var n) || n < 1)
            {
                chat.PrintError("[Little Luxuries] Usage: /enterhouse [apartment number]");
                return;
            }
            targetRoom = n - 1;
        }

        var door = Nearest(o => o.BaseId == ApartmentEntranceDataId || (o.IsTargetable && o.ObjectKind == ObjectKind.EventObj && o.Name.TextValue.Contains("entrance", StringComparison.OrdinalIgnoreCase)));

        if (door is null) { chat.PrintError("[Little Luxuries] No house or apartment entrance nearby."); return; }
        if (targetRoom is not null && door.BaseId != ApartmentEntranceDataId)
        {
            chat.PrintError("[Little Luxuries] A room number only works at an apartment building entrance.");
            return;
        }

        Svc.Targets.Target = door;
        TargetSystem.Instance()->InteractWithObject(door.Struct(), false);
        entering = true;
        enterFrames = 0;
    }

    private void OnItemAction(string command, string args)
    {
        if (!configuration.QuickCommands) return;

        var name = args.Trim().Trim('"');
        if (name.Length == 0) { chat.PrintError("[Little Luxuries] Usage: /itemaction <item name>."); return; }

        var item = Svc.Data.GetExcelSheet<Item>().FirstOrDefault(i => i.Name.ExtractText().Equals(name, StringComparison.OrdinalIgnoreCase));

        if (item.RowId == 0) { chat.PrintError($"[Little Luxuries] No item named \"{name}\"."); return; }

        AgentInventoryContext.Instance()->UseItem(item.RowId);
    }

    private void OnFrameworkUpdate(IFramework _)
{
    if (!entering) return;

    if (Ready("SelectYesno", out var yn))
    {
        new AddonMaster.SelectYesno((nint)yn).Yes();
        entering = false;
        return;
    }

    if (targetRoom is int room && Ready("MansionSelectRoom", out var mr))
    {
        if (mr->AtkValues[0].UInt != 4) return;

        var section = room / 15;
        var slot = room % 15;
        if (mr->AtkValues[1].Int != section)
        {
            if (EzThrottler.Throttle("QC.ApartmentSection", 2000)) ECommons.Automation.Callback.Fire(mr, true, 1, section);
        }
        else if (EzThrottler.Throttle("QC.ApartmentRoom", 2000)) ECommons.Automation.Callback.Fire(mr, true, 0, slot);

        return;
    }

    if (Ready("SelectString", out var ss))
    {
        var entries = new AddonMaster.SelectString((nint)ss).Entries;
        var idx = targetRoom is null
            ? Array.FindIndex(entries, e => e.Text.Contains("apartment", StringComparison.OrdinalIgnoreCase) && !e.Text.Contains("specified", StringComparison.OrdinalIgnoreCase))
            : Array.FindIndex(entries, e => e.Text.Contains("specified", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && EzThrottler.Throttle("QC.SelectApt", 2000))
        {
            entries[idx].Select();
            return;
        }
    }

    if (++enterFrames > 300) entering = false;
}

    public override void DrawConfig()
    {
        var enabled = configuration.QuickCommands;
        if (ImGui.Checkbox("Quick command shortcuts", ref enabled))
        {
            configuration.QuickCommands = enabled;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Enables /targetnearest, /acceptduty, /enterhouse, and /itemaction. When off, the commands do nothing.");

        ImGui.Spacing();
        ImGui.TextWrapped("/targetnearest - target the closest interactable object (housing door, entrance, aetheryte, etc.).");
        ImGui.TextWrapped("/acceptduty - press Commence on the Duty Ready popup.");
        ImGui.TextWrapped("/enterhouse - interact with the nearest house/apartment entrance and confirm entry automatically.");
        ImGui.TextWrapped("/itemaction <item name> - use an inventory item by its name (potions, food, prisms etc)");
    }
}
