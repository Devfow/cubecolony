using System;

namespace CheapNeuroSim
{

public struct StableRandom
{
    private uint _state;

    public StableRandom(uint seed)
    {
        _state = seed == 0 ? 0xA341316Cu : seed;
    }

    public uint NextUInt()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
    }

    public float NextFloat(float minInclusive, float maxInclusive)
    {
        var unit = (NextUInt() >> 8) * (1f / 16777215f);
        return minInclusive + (maxInclusive - minInclusive) * unit;
    }

    public bool Chance(float probability)
    {
        return NextFloat(0f, 1f) < MathUtil.Clamp01(probability);
    }

    public float NextGaussian()
    {
        var u1 = Math.Max(NextFloat(0f, 1f), 0.000001f);
        var u2 = NextFloat(0f, 1f);
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}

}