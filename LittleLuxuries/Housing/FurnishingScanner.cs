using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Serilog;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace LittleLuxuries.Housing;

public sealed class FurnishingScanner
{
    private readonly IObjectTable _objects;

    public FurnishingScanner(IObjectTable objects) => _objects = objects;

    public IEnumerable<Furnishing> Enumerate()
    {
        foreach (var obj in _objects)
        {
            if (obj.ObjectKind != ObjectKind.HousingEventObject) continue;

            FurnishingId id;
            unsafe
            {
                var housingObject = (HousingObject*)obj.Address;
                var gameObject = (GameObject*)obj.Address;

                id = FurnishingId.Compute(housingObject->HousingObjectId.Id, obj.Position, obj.Rotation);
                Log.Information($"default={gameObject->DefaultPosition}  live={obj.Position}  rot={obj.Rotation}");
            }

            yield return new Furnishing(id, obj.Name.TextValue, obj.Address);
        }
    }
}
