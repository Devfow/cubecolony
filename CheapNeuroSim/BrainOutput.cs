using System;

namespace CheapNeuroSim
{

public sealed class BrainOutput
{
    private readonly float[] _actions;

    public BrainOutput(int actionCount)
    {
        if (actionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actionCount));
        }

        _actions = new float[actionCount];
    }

    public int Count => _actions.Length;

    public float this[int index] => _actions[index];

    public ReadOnlySpan<float> Actions => _actions;

    internal Span<float> MutableActions => _actions;

    internal void Clear()
    {
        Array.Clear(_actions, 0, _actions.Length);
    }
}

}