using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices.Legacy;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LittleLuxuries.UI;

namespace LittleLuxuries.Tweaks;

public sealed unsafe class CommendQueue : Tweak, IDisposable
{
    private readonly IContextMenu contextMenu;
    private readonly ICondition condition;
    private readonly IChatGui chat;
    private readonly IClientState clientState;
    private readonly IPartyList partyList;
    private readonly IFramework framework;
    private readonly Configuration configuration;

    private (string Name, uint WorldId)? queued;
    private bool triedOpen;
    private int selectedRow = -1;
    private int commitFrames;

    private readonly HashSet<string> premadeNames = new();

    public CommendQueue(IContextMenu contextMenu, ICondition condition, IChatGui chat,
                        IClientState clientState, IPartyList partyList, IFramework framework, Configuration configuration)
    {
        this.contextMenu = contextMenu;
        this.condition = condition;
        this.chat = chat;
        this.clientState = clientState;
        this.partyList = partyList;
        this.framework = framework;
        this.configuration = configuration;

        contextMenu.OnMenuOpened += OnMenuOpened;
        condition.ConditionChange += OnConditionChange;
        framework.Update += OnFrameworkUpdate;
    }

    public override string Name => "Commend Queue";

    public override string Description =>
        "Lets you queue the player you want to commend during a duty, swapping or cancelling your pick at any time, " +
        "then awards the commendation automatically when the duty ends.";

    public override bool IsImplemented => true;

    public void Dispose()
    {
        contextMenu.OnMenuOpened -= OnMenuOpened;
        condition.ConditionChange -= OnConditionChange;
        framework.Update -= OnFrameworkUpdate;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (!configuration.CommendQueue) return;
        if (!condition[ConditionFlag.BoundByDuty]) return;
        if (args.AddonName != "_PartyList") return;
        if (args.Target is not MenuTargetDefault t || string.IsNullOrEmpty(t.TargetName)) return;
        if (t.TargetName == clientState.LocalPlayer?.Name.TextValue) return;
        if (premadeNames.Contains(t.TargetName)) return;

        var isQueued = queued is { } q && q.Name == t.TargetName && q.WorldId == t.TargetHomeWorld.RowId;

        args.AddMenuItem(new MenuItem
        {
            Name = isQueued ? "Cancel Commendation" : "Commend at Duty End",
            Prefix = SeIconChar.BoxedLetterL,
            PrefixColor = 541,
            OnClicked = _ =>
            {
                if (isQueued)
                {
                    queued = null;
                    chat.Print($"[Little Luxuries] Cancelled commendation for {t.TargetName}.");
                }
                else
                {
                    queued = (t.TargetName, t.TargetHomeWorld.RowId);
                    chat.Print($"[Little Luxuries] Will commend {t.TargetName} at the end of the duty.");
                }
            }
        });
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        if (queued is not { } target || !configuration.CommendQueue)
        {
            triedOpen = false; selectedRow = -1; commitFrames = 0;
            return;
        }

        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("VoteMvp", out var vote))
        {
            if (!GenericHelpers.IsAddonReady(vote)) return;
            var m = new AddonMaster.VoteMvp((nint)vote);

            if (selectedRow < 0)
            {
                for (var r = 0; r < 7; r++)
                {
                    var name = vote->AtkValues[9 + r];
                    if (name.Type != AtkValueType.String || name.String.ToString() != target.Name) continue;

                    var btn = vote->GetComponentButtonById((uint)(3 + r));
                    if (btn == null) break;

                    ((AtkComponentRadioButton*)btn)->IsSelected = true;
                    var evt = btn->AtkComponentBase.OwnerNode->AtkResNode.AtkEventManager.Event;
                    vote->ReceiveEvent(AtkEventType.ButtonClick, 3 + r, evt, null);
                    selectedRow = r;
                    return;
                }

                queued = null;
                return;
            }

            if (m.OkButton != null && m.OkButton->IsEnabled)
            {
                var okEvt = m.OkButton->AtkComponentBase.OwnerNode->AtkResNode.AtkEventManager.Event;
                vote->ReceiveEvent(AtkEventType.ButtonClick, 0, okEvt, null);
                chat.Print($"[Little Luxuries] Commended {target.Name}.");
                queued = null; selectedRow = -1;
            }
            else if (++commitFrames > 300)
            {
                queued = null; selectedRow = -1;
            }
            return;
        }

        selectedRow = -1; commitFrames = 0;

        if (condition[ConditionFlag.WatchingCutscene] || condition[ConditionFlag.WatchingCutscene78]
            || condition[ConditionFlag.OccupiedInCutSceneEvent])
        {
            triedOpen = false;
            return;
        }

        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("_NotificationIcMvp", out var _mvp)) { triedOpen = false; return; }
        if (triedOpen) return;
        triedOpen = true;

        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("_Notification", out var notification)) return;

        var openValues = stackalloc AtkValue[2];
        openValues[0].SetInt(0);
        openValues[1].SetInt(11);
        notification->FireCallback(2, openValues);
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.WaitingForDuty && value)
        {
            queued = null;
            premadeNames.Clear();
            foreach (var pm in partyList) premadeNames.Add(pm.Name.TextValue);
        }
    }

    public override void DrawConfig()
    {
        var inDuty = condition[ConditionFlag.BoundByDuty];

        ImGui.BeginDisabled(inDuty);
        var enabled = configuration.CommendQueue;
        if (ImGui.Checkbox("Queue commendations from the party list", ref enabled))
        {
            configuration.CommendQueue = enabled;
            configuration.Save();
        }
        ImGui.EndDisabled();

        ImGuiUtil.Tooltip(inDuty
            ? "Can't be changed while you're in a duty. Leave the duty to toggle it."
            : "Adds a \"Commend at Duty End\" entry when you right-click a party member. The queued player is commended automatically when the duty ends.");

        ImGui.Spacing();
        ImGui.TextWrapped("Right-click a party member and choose \"Commend at Duty End\". " +
                          "Right-click them again to cancel, or pick someone else to swap. " +
                          "Yourself and anyone already in your party when you queued can't be commended, so they aren't offered.");
    }
}
