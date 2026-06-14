using System.Collections.Generic;
using Dalamud.Plugin.Services;
using LittleLuxuries.Models.Housing;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace LittleLuxuries.Services.Housing;

public sealed class FurnishingScanner
{
    private readonly IObjectTable _objects;

    public FurnishingScanner(IObjectTable objects) => _objects = objects;

    public IEnumerable<Furnishing> Enumerate()
    {
        foreach (var obj in _objects)
        {
            if (obj.ObjectKind != ObjectKind.HousingEventObject) continue;

            yield return new Furnishing(FurnishingId.From(obj), obj.Name.TextValue, obj.Address);
        }
    }
}
