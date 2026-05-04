using System;

namespace CheapNeuroSim
{

public readonly struct BrainInput
{
    private readonly float[] _values;

    public BrainInput(float[] values)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public int Count => _values.Length;

    public float this[int index] => _values[index];

    public ReadOnlySpan<float> Values => _values;

    public static BrainInput FromArray(float[] values) => new(values);
}

}