using System;

namespace CheapNeuroSim
{

internal static class MathUtil
{
    public static float Clamp01(float value) => Clamp(value, 0f, 1f);

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Approach(float current, float target, float rate)
    {
        rate = Clamp01(rate);
        return current + (target - current) * rate;
    }

    public static float FastTanh(float x)
    {
        if (x < -3f) return -1f;
        if (x > 3f) return 1f;
        return x * (27f + x * x) / (27f + 9f * x * x);
    }
}

}