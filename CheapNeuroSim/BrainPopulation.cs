using System;
using System.Collections.Generic;

namespace CheapNeuroSim
{

public sealed class BrainPopulation
{
    private readonly List<Brain> _brains;

    public BrainPopulation(IEnumerable<Brain> brains)
    {
        _brains = new List<Brain>(brains ?? throw new ArgumentNullException(nameof(brains)));
    }

    public IReadOnlyList<Brain> Brains => _brains;

    public void ExchangeSignals(float influence = 0.12f)
    {
        if (_brains.Count < 2)
        {
            return;
        }

        var signals = new SocialSignal[_brains.Count];
        for (var i = 0; i < _brains.Count; i++)
        {
            signals[i] = _brains[i].EmitSignal();
        }

        for (var i = 0; i < _brains.Count; i++)
        {
            var left = signals[(i + _brains.Count - 1) % _brains.Count];
            var right = signals[(i + 1) % _brains.Count];
            _brains[i].ReceiveSignal(left, influence * 0.5f);
            _brains[i].ReceiveSignal(right, influence * 0.5f);
        }
    }
}

}