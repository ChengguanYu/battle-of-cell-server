namespace Entity.Utils;

public sealed class RecyclableUIntIdGeneratorEntity
{
    public readonly HashSet<uint> Occupied = new();
    public readonly List<uint> Free = new();
    public uint MinInclusive = 1;
    public uint MaxExclusive = uint.MaxValue;
    public uint NextId = 1;
}
