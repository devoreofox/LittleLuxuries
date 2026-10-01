using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LittleLuxuries.Models.Housing;

namespace LittleLuxuries.Services.Housing;

public sealed unsafe class EstateRegistry : IDisposable
{
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly Configuration configuration;
    private readonly EstateAddresses addresses;

    public EstateRegistry(IClientState clientState, IPlayerState playerState, Configuration configuration, EstateAddresses addresses)
    {
        this.clientState = clientState;
        this.playerState = playerState;
        this.configuration = configuration;
        this.addresses = addresses;

        clientState.Login += Track;

        if (clientState.IsLoggedIn) Track();
    }

    public void Dispose() => clientState.Login -= Track;

    public EstateLabelCharacter? Current => configuration.EstateLabels.GetValueOrDefault(playerState.ContentId);

    public void Track()
    {
        if (!configuration.PersonalEstateLabels) return;

        var charID = playerState.ContentId;
        if (charID == 0) return;
        if (!configuration.EstateLabels.TryGetValue(charID, out var character)) configuration.EstateLabels[charID] = character = new EstateLabelCharacter();
        character.Name = $"{playerState.CharacterName} @ {playerState.HomeWorld.Value.Name.ExtractText()}";
        configuration.Save();
    }

    public void Refresh()
    {
        if (Current is not { } character) return;

        var changed = false;
        foreach (var t in Telepo.Instance()->TeleportList.AsSpan())
        {
            if (t.EstateType == (EstateType)255) continue;

            var location = addresses.FormatLocation(t.HouseId, addresses.PlaceName(t.TerritoryId));

            if (character.Estates.TryGetValue(t.HouseId.Id, out var existing))
            {
                if (existing.Location == location) continue;
                existing.Location = location;
            }
            else
            {
                character.Estates[t.HouseId.Id] = new EstateLabel { Location = location, Order = character.Estates.Count };
            }
            changed = true;
        }

        if (changed) configuration.Save();
    }
}
