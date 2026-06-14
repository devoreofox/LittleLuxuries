using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace LittleLuxuries.Services.Dpose;

public class CposeController : IDisposable
{
    private readonly IClientState clientstate;
    private readonly IFramework framework;

    private CancellationTokenSource? _cts;
    private const int PaceMs = 150; // TODO make this configurable

    private static readonly HashSet<EmoteController.PoseType> Poseable = new()
    {
        EmoteController.PoseType.Idle,
        EmoteController.PoseType.Sit,
        EmoteController.PoseType.GroundSit,
        EmoteController.PoseType.Doze
    };

    public CposeController(IClientState clientstate, IFramework framework)
    {
        this.clientstate = clientstate;
        this.framework = framework;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public bool IsPoseable()
    {
        var type = GetCurrentPoseType();
        return type is not null && Poseable.Contains(type.Value);
    }

    private unsafe Character* LocalCharacter()
    {
        var player = clientstate.LocalPlayer;
        return player is null ? null : (Character*)player.Address;
    }

    public unsafe byte? GetCurrentPose()
    {
        var character = LocalCharacter();
        if (character is null) return null;
        return character->EmoteController.CPoseState;
    }

    public unsafe EmoteController.PoseType? GetCurrentPoseType()
    {
        var character = LocalCharacter();
        if (character is null) return null;
        return character->EmoteController.CurrentPoseType;
    }

    public byte GetMaxPose(EmoteController.PoseType type) => EmoteController.GetAvailablePoses(type);

    public void DriveTo(byte target)
    {
        var type = GetCurrentPoseType();
        if (type is null || !Poseable.Contains(type.Value) || target > GetMaxPose(type.Value)) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _ = DriveLoopAsync(target, type.Value, _cts.Token);
    }

    private async Task DriveLoopAsync(byte target, EmoteController.PoseType type, CancellationToken token)
    {
        var maxSteps = (GetMaxPose(type) + 1) * 2;
        try
        {
            for (var i = 0; i < maxSteps; i++)
            {
                if (token.IsCancellationRequested) return;

                var stop = await framework.RunOnFrameworkThread(() =>
                {
                    if (GetCurrentPoseType() != type) return true;
                    if (GetCurrentPose() == target) return true;
                    Chat.SendMessage("/cpose");

                    return false;
                });

                if (stop) return;
                await Task.Delay(PaceMs, token);
            }
        }
        catch (OperationCanceledException){ }
        catch (Exception ex) { Serilog.Log.Error(ex, "cpose drive failed"); }
    }
}
