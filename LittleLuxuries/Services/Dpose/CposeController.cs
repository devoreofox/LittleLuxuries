using Dalamud.Plugin.Services;
using ECommons.DalamudServices.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace LittleLuxuries.Services.Dpose;

public unsafe class CposeController
{
    private readonly IClientState clientstate;

    public CposeController(IClientState clientstate) => this.clientstate = clientstate;

    private Character* LocalCharacter()
    {
        var player = clientstate.LocalPlayer;
        return player is null ? null : (Character*)player.Address;
    }

    public byte? GetCurrentPose()
    {
        var character = LocalCharacter();
        if (character is null) return null;
        return character->EmoteController.CPoseState;
    }

    public EmoteController.PoseType? GetCurrentPoseType()
    {
        var character = LocalCharacter();
        if (character is null) return null;
        return character->EmoteController.CurrentPoseType;
    }

    public byte GetMaxPose(EmoteController.PoseType type) => EmoteController.GetAvailablePoses(type);
}
