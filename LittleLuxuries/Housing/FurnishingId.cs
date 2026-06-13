using System;
using System.Numerics;

namespace LittleLuxuries.Housing;

public readonly struct FurnishingId : IEquatable<FurnishingId>
{
    public uint Value { get; }

    public FurnishingId(uint value) => Value = value;

    public static FurnishingId Compute(uint typeId, Vector3 position, float rotation)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ typeId) * 16777619u;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(position.X)) * 16777619u;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(position.Y)) * 16777619u;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(position.Z)) * 16777619u;
            hash = (hash ^ BitConverter.SingleToUInt32Bits(rotation))  * 16777619u;
            return new FurnishingId(hash);
        }
    }

    public bool Equals(FurnishingId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is FurnishingId id && Equals(id);

    public override int GetHashCode() => (int)Value;

    public static bool operator ==(FurnishingId a, FurnishingId b) => a.Value == b.Value;
    public static bool operator !=(FurnishingId a, FurnishingId b) => a.Value != b.Value;

    public override string ToString() => $"0x{Value:X8}";


}
