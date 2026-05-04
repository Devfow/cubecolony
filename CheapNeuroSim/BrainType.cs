using System;

namespace CheapNeuroSim
{

public sealed class BrainType
{
    public BrainType(
        string id,
        int sensorCount,
        int actionCount,
        int neuronCount,
        int synapseCount,
        ChemicalProfile chemicalProfile,
        float baselinePlasticity = 0.02f)
        : this(id, sensorCount, actionCount, neuronCount, synapseCount, chemicalProfile, NeedsProfile.CreatureDefault(), baselinePlasticity)
    {
    }

    public BrainType(
        string id,
        int sensorCount,
        int actionCount,
        int neuronCount,
        int synapseCount,
        ChemicalProfile chemicalProfile,
        NeedsProfile needsProfile,
        float baselinePlasticity = 0.02f)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Brain type id is required.", nameof(id));
        if (sensorCount <= 0) throw new ArgumentOutOfRangeException(nameof(sensorCount));
        if (actionCount <= 0) throw new ArgumentOutOfRangeException(nameof(actionCount));
        if (neuronCount <= 0) throw new ArgumentOutOfRangeException(nameof(neuronCount));
        if (synapseCount < neuronCount) throw new ArgumentOutOfRangeException(nameof(synapseCount));

        Id = id;
        SensorCount = sensorCount;
        ActionCount = actionCount;
        NeuronCount = neuronCount;
        SynapseCount = synapseCount;
        ChemicalProfile = chemicalProfile;
        NeedsProfile = needsProfile;
        BaselinePlasticity = MathUtil.Clamp(baselinePlasticity, 0f, 1f);
    }

    public string Id { get; }
    public int SensorCount { get; }
    public int ActionCount { get; }
    public int NeuronCount { get; }
    public int SynapseCount { get; }
    public ChemicalProfile ChemicalProfile { get; }
    public NeedsProfile NeedsProfile { get; }
    public float BaselinePlasticity { get; }

    public static BrainType FirstGenerationCreature()
    {
        return new BrainType(
            "first_generation_creature",
            sensorCount: 8,
            actionCount: 8,
            neuronCount: 32,
            synapseCount: 128,
            chemicalProfile: ChemicalProfile.CreatureDefault(),
            baselinePlasticity: 0.025f);
    }
}

}
