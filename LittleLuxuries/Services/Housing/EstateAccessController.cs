using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices.Legacy;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace LittleLuxuries.Services.Housing;

public unsafe class EstateAccessController : IDisposable
{
    private delegate byte OpenEstateAccessDelegate(AgentHousing* agent);
    [Signature("E8 ?? ?? ?? ?? 84 C0 0F 85 ?? ?? ?? ?? 40 32 F6 E9 ?? ?? ?? ?? E8")]
    private readonly OpenEstateAccessDelegate openEstateAccess = null!;

    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly IAddonLifecycle addonLifecycle;
    private bool? _pendingPrivate;
    private bool? _pendingTeleport;
    private bool _pending;

    [StructLayout(LayoutKind.Explicit, Size = 0xDE90)]
    private struct AgentHousing { [FieldOffset(0xA734)] public SelectedEstateType SelectedEstateType; }
    private enum SelectedEstateType : byte { PersonalEstate, FreeCompanyEstate, PersonalChambers, ApartmentRoom }

    public EstateAccessController(IClientState clientState, ICondition condition, IAddonLifecycle addonLifecycle, IGameInteropProvider interop)
    {
        this.clientState = clientState;
        this.condition = condition;
        this.addonLifecycle = addonLifecycle;
        interop.InitializeFromAttributes(this);
        addonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingConfig", OnConfigSetup);
    }

    public void Dispose() => addonLifecycle.UnregisterListener(OnConfigSetup);

    public bool CanControl()
    {
        var player = clientState.LocalPlayer;
        if (player is null) return false;
        if (player.CurrentWorld.RowId != player.HomeWorld.RowId) return false;
        if (condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]) return false;
        return GameMain.Instance()->CurrentTerritoryIntendedUseId is not (TerritoryIntendedUse.Eureka or TerritoryIntendedUse.Bozja or TerritoryIntendedUse.OccultCrescent);
    }

    public bool TrySetGuestAccess(EstateType? estate, bool? privateAccess, bool? teleport)
    {
        if (openEstateAccess is null || !CanControl()) return false;
        if (privateAccess is null && teleport is null) return false;

        var target = estate ?? FirstOwnedEstate();
        if (target is null) return false;

        var agent = (AgentHousing*)AgentModule.Instance()->GetAgentByInternalId(AgentId.Housing);
        if (agent is null) return false;
        agent->SelectedEstateType = Map(target.Value);

        _pendingPrivate = privateAccess;
        _pendingTeleport = teleport;
        _pending = true;
        openEstateAccess(agent);
        return true;
    }

    private void OnConfigSetup(AddonEvent type, AddonArgs args)
    {
        if (!_pending) return;
        _pending = false;

        var setup = (AddonSetupArgs)args;
        var current = ((AtkValue*)setup.AtkValues)[1].UInt;
        var currentPrivate = (current & 1) != 0;
        var currentTeleport = (current & 2) != 0;

        var wantPrivate = _pendingPrivate  ?? currentPrivate;
        var wantTeleport = _pendingTeleport ?? currentTeleport;
        var mask = (uint)((wantPrivate ? 1 : 0) | (wantTeleport ? 2 : 0));

        var addon = (AtkUnitBase*)args.Addon.Address;
        var values = stackalloc AtkValue[5];
        values[0].SetInt(0);
        values[1].SetUInt(mask);
        addon->FireCallback(5, values, true);

        var housing = AgentModule.Instance()->GetAgentByInternalId(AgentId.Housing);
        if (housing != null) housing->Hide();
    }

    private static EstateType? FirstOwnedEstate()
    {
        foreach (var t in new[] { EstateType.PersonalEstate, EstateType.ApartmentRoom, EstateType.PersonalChambers, EstateType.FreeCompanyEstate })
            if (HousingManager.GetOwnedHouseId(t).Id != 0) return t;
        return null;
    }

    private static SelectedEstateType Map(EstateType e) => e switch
    {
        EstateType.FreeCompanyEstate => SelectedEstateType.FreeCompanyEstate,
        EstateType.PersonalChambers => SelectedEstateType.PersonalChambers,
        EstateType.ApartmentRoom => SelectedEstateType.ApartmentRoom,
        _ => SelectedEstateType.PersonalEstate,
    };
}
