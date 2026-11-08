using UnityEngine;

/// <summary>Stable route-seeded variation with adjacent-repeat avoidance.</summary>
public static class RunnerEnvironmentVariation
{
    public static int SelectIndex(int regionId, int sequenceSlot, int variantCount, int seed)
    {
        if (variantCount <= 1) return 0;

        int current = PositiveModulo(Hash(regionId, sequenceSlot, seed), variantCount);
        if (sequenceSlot <= 0) return current;

        int previous = SelectIndex(regionId, sequenceSlot - 1, variantCount, seed);
        if (current == previous)
        {
            int offset = 1 + PositiveModulo(Hash(regionId ^ 0x51, sequenceSlot, seed), variantCount - 1);
            current = (previous + offset) % variantCount;
        }

        return current;
    }

    private static int Hash(int regionId, int slot, int seed)
    {
        unchecked
        {
            uint value = (uint)seed;
            value ^= (uint)(regionId + 1) * 0x9E3779B9u;
            value ^= (uint)(slot + 1) * 0x85EBCA6Bu;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (int)(value & 0x7FFFFFFF);
        }
    }

    private static int PositiveModulo(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
