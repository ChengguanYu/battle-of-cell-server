using Entity.Utils;

namespace Hotfix.Scene.Rooms.System;

public static class RecyclableUIntIdGeneratorEntitySystem
{
    public static bool TryAcquire(this RecyclableUIntIdGeneratorEntity self, out uint id)
    {
        if (self.Free.Count > 0)
        {
            var last = self.Free.Count - 1;
            id = self.Free[last];
            self.Free.RemoveAt(last);
            self.Occupied.Add(id);
            return true;
        }

        if (self.NextId >= self.MaxExclusive)
        {
            id = 0;
            return false;
        }

        id = self.NextId;
        self.NextId++;
        self.Occupied.Add(id);
        return true;
    }

    public static bool Release(this RecyclableUIntIdGeneratorEntity self, uint id)
    {
        if (id < self.MinInclusive || id >= self.MaxExclusive)
        {
            return false;
        }

        if (!self.Occupied.Remove(id))
        {
            return false;
        }

        self.Free.Add(id);
        return true;
    }

    public static bool IsOccupied(this RecyclableUIntIdGeneratorEntity self, uint id)
    {
        return self.Occupied.Contains(id);
    }
}
