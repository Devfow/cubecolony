using System;

namespace CheapNeuroSim
{

public sealed class BrainGenome
{
    public BrainGenome(BrainType type, ChemicalProfile chemicals, NeuronGene[] neurons, SynapseGene[] synapses)
        : this(type, chemicals, TemperamentProfile.CreatureDefault(), neurons, synapses)
    {
    }

    public BrainGenome(BrainType type, ChemicalProfile chemicals, TemperamentProfile temperament, NeuronGene[] neurons, SynapseGene[] synapses)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Chemicals = chemicals;
        Temperament = temperament;
        Neurons = neurons ?? throw new ArgumentNullException(nameof(neurons));
        Synapses = synapses ?? throw new ArgumentNullException(nameof(synapses));
        if (neurons.Length != type.NeuronCount) throw new ArgumentException("Neuron count must match brain type.", nameof(neurons));
        if (synapses.Length != type.SynapseCount) throw new ArgumentException("Synapse count must match brain type.", nameof(synapses));
    }

    public BrainType Type { get; }
    public ChemicalProfile Chemicals { get; }
    public TemperamentProfile Temperament { get; }
    public NeuronGene[] Neurons { get; }
    public SynapseGene[] Synapses { get; }

    public static BrainGenome CreateFirstGeneration(uint seed = 1)
    {
        return CreateSeeded(BrainType.FirstGenerationCreature(), seed);
    }

    public static BrainGenome CreateSeeded(BrainType type, uint seed)
    {
        var random = new StableRandom(seed);
        var neurons = new NeuronGene[type.NeuronCount];
        for (var i = 0; i < neurons.Length; i++)
        {
            neurons[i] = new NeuronGene
            {
                Bias = random.NextFloat(-0.14f, 0.14f),
                Excitability = random.NextFloat(0.7f, 1.35f),
                Leak = random.NextFloat(0.04f, 0.18f),
                ActionTarget = i % type.ActionCount,
                ActionWeight = random.NextFloat(-0.6f, 0.9f)
            };
        }

        var synapses = new SynapseGene[type.SynapseCount];
        for (var i = 0; i < synapses.Length; i++)
        {
            synapses[i] = new SynapseGene
            {
                From = random.NextInt(0, type.NeuronCount),
                To = random.NextInt(0, type.NeuronCount),
                Weight = random.NextFloat(-0.8f, 0.8f),
                Plasticity = random.NextFloat(0.0f, 0.08f)
            };
        }

        WireInnateSurvivalCircuits(type, neurons, synapses);
        return new BrainGenome(type, type.ChemicalProfile, TemperamentProfile.CreatureDefault(), neurons, synapses);
    }

    public BrainGenome Mutated(uint seed, MutationSettings? settings = null)
    {
        settings ??= MutationSettings.Conservative();
        var random = new StableRandom(seed);
        var neurons = new NeuronGene[Neurons.Length];
        var synapses = new SynapseGene[Synapses.Length];
        Array.Copy(Neurons, neurons, Neurons.Length);
        Array.Copy(Synapses, synapses, Synapses.Length);

        for (var i = 0; i < neurons.Length; i++)
        {
            neurons[i].Bias = BlendClamped(neurons[i].Bias, neurons[i].Bias + random.NextGaussian() * settings.BiasSigma, -1f, 1f, settings.SurvivalBlend);
            neurons[i].Excitability = BlendClamped(neurons[i].Excitability, neurons[i].Excitability + random.NextGaussian() * settings.WeightSigma, 0.2f, 2.5f, settings.SurvivalBlend);
            neurons[i].Leak = BlendClamped(neurons[i].Leak, neurons[i].Leak + random.NextGaussian() * settings.WeightSigma * 0.3f, 0.005f, 0.6f, settings.SurvivalBlend);
            neurons[i].ActionWeight = BlendClamped(neurons[i].ActionWeight, neurons[i].ActionWeight + random.NextGaussian() * settings.WeightSigma, -1.5f, 1.5f, settings.SurvivalBlend);
            if (random.Chance(settings.ActionMutationChance))
            {
                neurons[i].ActionTarget = random.NextInt(0, Type.ActionCount);
            }
        }

        for (var i = 0; i < synapses.Length; i++)
        {
            synapses[i].Weight = BlendClamped(synapses[i].Weight, synapses[i].Weight + random.NextGaussian() * settings.WeightSigma, -2f, 2f, settings.SurvivalBlend);
            synapses[i].Plasticity = BlendClamped(synapses[i].Plasticity, synapses[i].Plasticity + random.NextGaussian() * settings.WeightSigma * 0.12f, 0f, 0.25f, settings.SurvivalBlend);
            if (random.Chance(settings.RewireChance))
            {
                synapses[i].From = random.NextInt(0, Type.NeuronCount);
            }

            if (random.Chance(settings.RewireChance))
            {
                synapses[i].To = random.NextInt(0, Type.NeuronCount);
            }
        }

        if (random.Chance(settings.StructuralMutationChance))
        {
            var index = random.NextInt(0, synapses.Length);
            synapses[index] = new SynapseGene
            {
                From = random.NextInt(0, Type.NeuronCount),
                To = random.NextInt(0, Type.NeuronCount),
                Weight = random.NextFloat(-0.65f, 0.65f),
                Plasticity = random.NextFloat(0.0f, 0.06f)
            };
        }

        return new BrainGenome(Type, Chemicals.Mutated(ref random, settings), Temperament.Mutated(ref random, settings), neurons, synapses);
    }

    public Brain CreateBrain() => new(this);

    private static float BlendClamped(float original, float mutated, float min, float max, float keepOriginal)
    {
        return MathUtil.Clamp(original * keepOriginal + mutated * (1f - keepOriginal), min, max);
    }

    private static void WireInnateSurvivalCircuits(BrainType type, NeuronGene[] neurons, SynapseGene[] synapses)
    {
        if (type.SensorCount < 6 || type.ActionCount < 7 || neurons.Length < 8 || synapses.Length < 8)
        {
            return;
        }

        neurons[0].ActionTarget = BrainChannels.Eat;
        neurons[0].ActionWeight = 1.0f;
        neurons[1].ActionTarget = BrainChannels.Flee;
        neurons[1].ActionWeight = 1.0f;
        neurons[2].ActionTarget = BrainChannels.Rest;
        neurons[2].ActionWeight = 0.8f;
        neurons[3].ActionTarget = BrainChannels.Explore;
        neurons[3].ActionWeight = 0.7f;
        neurons[4].ActionTarget = BrainChannels.Signal;
        neurons[4].ActionWeight = 0.6f;

        synapses[0] = new SynapseGene { From = 0, To = 0, Weight = 1.1f, Plasticity = 0.03f };
        synapses[1] = new SynapseGene { From = 1, To = 1, Weight = 1.2f, Plasticity = 0.02f };
        synapses[2] = new SynapseGene { From = 5, To = 2, Weight = 0.7f, Plasticity = 0.02f };
        synapses[3] = new SynapseGene { From = 4, To = 3, Weight = 0.7f, Plasticity = 0.04f };
        synapses[4] = new SynapseGene { From = 3, To = 4, Weight = 0.6f, Plasticity = 0.05f };
    }
}

}
