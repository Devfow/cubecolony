using System;

namespace CheapNeuroSim
{

public sealed class Brain
{
    private readonly BrainGenome _genome;
    private readonly float[] _activation;
    private readonly float[] _nextActivation;
    private readonly float[] _lastActivation;
    private readonly float[] _weights;
    private readonly BrainOutput _output;
    private readonly EpisodicMemoryState _memory;
    private readonly CultureState _culture;
    private readonly ReputationState _reputation;
    private AddictionState _addiction;
    private NeedsState _needs;
    private AttentionState _attention;
    private GoalState _goal;
    private EmotionState _emotion;
    private DevelopmentState _development;
    private TraumaState _trauma;
    private SocialBiasState _socialBias;
    private int _tick;

    public Brain(BrainGenome genome)
    {
        _genome = genome ?? throw new ArgumentNullException(nameof(genome));
        _activation = new float[genome.Type.NeuronCount];
        _nextActivation = new float[genome.Type.NeuronCount];
        _lastActivation = new float[genome.Type.NeuronCount];
        _weights = new float[genome.Synapses.Length];
        for (var i = 0; i < _weights.Length; i++)
        {
            _weights[i] = genome.Synapses[i].Weight;
        }

        _output = new BrainOutput(genome.Type.ActionCount);
        _memory = new EpisodicMemoryState();
        _culture = new CultureState();
        _reputation = new ReputationState();
        _needs = NeedsState.Balanced();
        _development = DevelopmentState.Newborn();
        Chemicals = NeurochemicalState.FromProfile(genome.Chemicals);
        _socialBias = new SocialBiasState(0, 0, SocialBiasSettings.LowBias());
    }

    public BrainType Type => _genome.Type;
    public NeurochemicalState Chemicals { get; private set; }
    public AddictionState Addiction => _addiction;
    public NeedsState Needs => _needs;
    public AttentionState Attention => _attention;
    public GoalState Goal => _goal;
    public EmotionState Emotion => _emotion;
    public DevelopmentState Development => _development;
    public TraumaState Trauma => _trauma;
    public EpisodicMemoryState Memory => _memory;
    public CultureState Culture => _culture;
    public ReputationState Reputation => _reputation;
    public TemperamentProfile Temperament => _genome.Temperament;
    public SocialBiasState SocialBias => _socialBias;
    public BrainOutput Output => _output;

    public BrainOutput Tick(BrainInput input, float reward, float dt = 1f)
    {
        if (input.Count != Type.SensorCount)
        {
            throw new ArgumentException("Input count must match the brain type.", nameof(input));
        }

        dt = MathUtil.Clamp(dt, 0.001f, 4f);
        _development.Tick(dt);
        _needs.TickSensors(input, Type.NeedsProfile, dt);
        _memory.Tick(dt);
        _attention.Update(input, _addiction, _needs, _genome.Temperament, dt);
        _trauma.RegisterStress(_attention.Threat, _attention.Pain, 1f - _needs.Energy, dt);
        _trauma.Recover(1f - _attention.Threat, _needs.Social, dt);
        var effectiveReward = MathUtil.Clamp(reward + _needs.RewardPressure(), -1f, 1f);
        var chemicals = Chemicals;
        chemicals.Tick(_genome.Chemicals, input, effectiveReward, dt);
        _addiction.Tick(ref chemicals, input, dt);
        chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + _needs.StressPressure() * 0.08f + _trauma.Hypervigilance * 0.05f);
        _socialBias.Tick(dt);
        Chemicals = chemicals;

        Array.Copy(_activation, _lastActivation, _activation.Length);
        Array.Clear(_nextActivation, 0, _nextActivation.Length);

        InjectSensors(input);
        PropagateSynapses();
        ActivateNeurons(dt);
        ProjectActions();
        _needs.TickActions(_output, dt);
        _goal.Update(_output, _needs, _addiction, _attention, dt);
        _emotion.Update(Chemicals, _addiction, _needs, _attention, _trauma);
        RecordAmbientMemory(input, effectiveReward);
        Learn(effectiveReward, dt);
        _tick++;

        return _output;
    }

    public BrainDebugSnapshot GetDebugSnapshot()
    {
        var activations = new float[_activation.Length];
        var actions = new float[_output.Count];
        Array.Copy(_activation, activations, _activation.Length);
        _output.Actions.CopyTo(actions);

        return new BrainDebugSnapshot(
            Type.Id,
            _tick,
            Chemicals,
            _addiction,
            _needs,
            _attention,
            _goal,
            _emotion,
            _development,
            _trauma,
            _genome.Temperament,
            PersonalityExpression.FromState(_genome.Temperament, _addiction, _trauma, _needs),
            activations,
            actions,
            CalculateSynapseStats(),
            _socialBias.GetDebugSnapshot(),
            _memory.GetDebugSnapshot(),
            _culture.GetDebugSnapshot(),
            _reputation.GetDebugSnapshot());
    }

    public BrainRuntimeState CaptureRuntimeState()
    {
        var activations = new float[_activation.Length];
        var weights = new float[_weights.Length];
        Array.Copy(_activation, activations, _activation.Length);
        Array.Copy(_weights, weights, _weights.Length);

        return new BrainRuntimeState
        {
            Tick = _tick,
            Chemicals = Chemicals,
            Addiction = _addiction,
            Needs = _needs,
            Attention = _attention,
            Goal = _goal,
            Emotion = _emotion,
            Development = _development,
            Trauma = _trauma,
            Activations = activations,
            LearnedWeights = weights,
            Memories = _memory.CopyTraces(),
            CultureBeliefs = _culture.CopyBeliefs(),
            ReputationEntries = _reputation.CopyEntries()
        };
    }

    public void RestoreRuntimeState(BrainRuntimeState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        _tick = state.Tick;
        Chemicals = state.Chemicals;
        _addiction = state.Addiction;
        _needs = state.Needs;
        _attention = state.Attention;
        _goal = state.Goal;
        _emotion = state.Emotion;
        _development = state.Development;
        _trauma = state.Trauma;

        Array.Clear(_activation, 0, _activation.Length);
        Array.Clear(_lastActivation, 0, _lastActivation.Length);
        if (state.Activations != null)
        {
            Array.Copy(state.Activations, _activation, Math.Min(state.Activations.Length, _activation.Length));
        }

        if (state.LearnedWeights != null)
        {
            Array.Copy(state.LearnedWeights, _weights, Math.Min(state.LearnedWeights.Length, _weights.Length));
        }

        _memory.Restore(state.Memories);
        _culture.Restore(state.CultureBeliefs);
        _reputation.Restore(state.ReputationEntries);
    }

    public void ApplyAddictiveStimulus(AddictiveStimulus stimulus)
    {
        var chemicals = Chemicals;
        _addiction.Apply(stimulus, ref chemicals);
        Chemicals = chemicals;
    }

    public void ConfigureSocialIdentity(int selfGroupId, int groupCount, SocialBiasSettings settings)
    {
        _socialBias = new SocialBiasState(groupCount, selfGroupId, settings);
    }

    public GroupAttitude ObserveSocialInteraction(SocialIdentity other, float reward, float threat, float dt = 1f)
    {
        dt = MathUtil.Clamp(dt, 0.001f, 4f);
        _socialBias.LearnFromInteraction(other, reward, threat, dt);
        var attitude = _socialBias.Evaluate(other);
        var recalled = _memory.RecallValence(other.GroupId);
        var chemicals = Chemicals;
        chemicals.Oxytocin = MathUtil.Clamp01(chemicals.Oxytocin + attitude.Affiliation * 0.08f + MathUtil.Clamp01(recalled) * 0.04f - attitude.Threat * 0.04f);
        chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + attitude.Threat * 0.12f + MathUtil.Clamp(-recalled, 0f, 1f) * 0.04f);
        chemicals.Norepinephrine = MathUtil.Clamp01(chemicals.Norepinephrine + attitude.Threat * 0.08f);
        Chemicals = chemicals;
        _memory.Record(other.GroupId, reward, threat, social: MathUtil.Clamp01(attitude.Affiliation + 0.5f), novelty: other.VisibleMarkerStrength);
        return attitude;
    }

    public void TeachCulture(int topicId, float valence, float confidence)
    {
        _culture.Teach(topicId, valence, confidence * _development.CultureUptakeMultiplier, _genome.Temperament.Sociability);
    }

    public CulturalMeme ExpressMeme(int topicId, float virulence = 0.5f)
    {
        var valence = _culture.GetValence(topicId);
        return new CulturalMeme(topicId, valence, Math.Abs(valence), virulence);
    }

    public void ReceiveMeme(CulturalMeme meme, SocialIdentity speaker, float prestige = 0.5f, uint mutationSeed = 1)
    {
        var attitude = _socialBias.Evaluate(speaker);
        var trust = MathUtil.Clamp01(prestige + attitude.Affiliation * 0.25f - attitude.Threat * 0.35f + Chemicals.Oxytocin * 0.15f);
        var received = meme.Mutated(mutationSeed);
        _culture.Teach(received, trust * _development.CultureUptakeMultiplier, _genome.Temperament.Sociability);
        _memory.Record(speaker.GroupId, received.Valence * trust, attitude.Threat, trust, received.Virulence);
    }

    public ReputationEntry ObserveIndividualInteraction(IndividualIdentity other, float helpfulness, float harm, float affection, float dt = 1f)
    {
        dt = MathUtil.Clamp(dt, 0.001f, 4f);
        var entry = _reputation.Learn(other, MathUtil.Clamp01(helpfulness), MathUtil.Clamp01(harm), MathUtil.Clamp(affection, -1f, 1f), dt);
        ObserveSocialInteraction(other.SocialIdentity, helpfulness - harm, harm, dt);
        return entry;
    }

    public void OfflineConsolidate(float restQuality, float dt = 1f)
    {
        restQuality = MathUtil.Clamp01(restQuality);
        dt = MathUtil.Clamp(dt, 0.001f, 16f);
        _trauma.Recover(restQuality, _needs.Social, dt * 1.8f);
        _memory.ConsolidateInto(_culture, restQuality);
        var chemicals = Chemicals;
        chemicals.Serotonin = MathUtil.Clamp01(chemicals.Serotonin + restQuality * 0.08f * dt);
        chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol - restQuality * 0.06f * dt);
        Chemicals = chemicals;
        _needs.TickActions(new BrainOutput(Type.ActionCount), dt * 0.25f);
    }

    public SocialSignal EmitSignal()
    {
        var actions = _output.Actions;
        var intensity = actions.Length > BrainChannels.Signal ? actions[BrainChannels.Signal] : 0f;
        return new SocialSignal(
            MathUtil.Clamp01(intensity),
            Chemicals.Oxytocin,
            Chemicals.Cortisol,
            Chemicals.Dopamine);
    }

    public void ReceiveSignal(SocialSignal signal, float influence = 0.18f)
    {
        influence = MathUtil.Clamp01(influence);
        var chemicals = Chemicals;
        chemicals.Oxytocin = MathUtil.Clamp01(chemicals.Oxytocin + signal.Affiliation * signal.Intensity * influence);
        chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + signal.Stress * signal.Intensity * influence * 0.7f);
        chemicals.Dopamine = MathUtil.Clamp01(chemicals.Dopamine + signal.Valence * signal.Intensity * influence * 0.5f);
        Chemicals = chemicals;
    }

    public void ReceiveSignal(SocialSignal signal, SocialIdentity sender, float influence = 0.18f)
    {
        var attitude = _socialBias.Evaluate(sender);
        var biasedInfluence = MathUtil.Clamp01(influence + attitude.Affiliation * 0.08f - attitude.Threat * 0.10f);
        ReceiveSignal(signal, biasedInfluence);

        var chemicals = Chemicals;
        chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + attitude.Threat * signal.Intensity * influence * 0.12f);
        chemicals.Oxytocin = MathUtil.Clamp01(chemicals.Oxytocin + attitude.Affiliation * signal.Intensity * influence * 0.08f);
        Chemicals = chemicals;
    }

    private void InjectSensors(BrainInput input)
    {
        var limit = Math.Min(input.Count, _nextActivation.Length);
        for (var i = 0; i < limit; i++)
        {
            _nextActivation[i] += MathUtil.Clamp(input[i], -1f, 1f) * _attention.GetSensorGain(i);
        }
    }

    private void PropagateSynapses()
    {
        var synapses = _genome.Synapses;
        for (var i = 0; i < synapses.Length; i++)
        {
            var synapse = synapses[i];
            _nextActivation[synapse.To] += _activation[synapse.From] * _weights[i];
        }
    }

    private void ActivateNeurons(float dt)
    {
        var calm = Chemicals.Serotonin * 0.25f;
        var arousal = 0.75f + Chemicals.Norepinephrine * (0.42f + _genome.Temperament.HarmAvoidance * 0.24f) - Chemicals.Cortisol * 0.22f + _attention.DominantSalience * 0.06f;
        for (var i = 0; i < _activation.Length; i++)
        {
            var gene = _genome.Neurons[i];
            var leaked = _activation[i] * MathUtil.Clamp01(1f - gene.Leak * dt);
            var raw = (leaked + _nextActivation[i] + gene.Bias - calm) * gene.Excitability * arousal;
            _activation[i] = MathUtil.FastTanh(raw);
        }
    }

    private void ProjectActions()
    {
        _output.Clear();
        var actions = _output.MutableActions;
        var drive = 0.7f + Chemicals.Dopamine * (0.32f + _genome.Temperament.NoveltySeeking * 0.25f) + Chemicals.Norepinephrine * 0.15f + (1f - _needs.Energy) * 0.08f;
        var inhibition = Chemicals.Serotonin * (0.10f + _genome.Temperament.ImpulseControl * _development.ImpulseControlMultiplier * 0.18f);
        for (var i = 0; i < _activation.Length; i++)
        {
            var gene = _genome.Neurons[i];
            actions[gene.ActionTarget] += _activation[i] * gene.ActionWeight * drive - inhibition;
        }

        for (var i = 0; i < actions.Length; i++)
        {
            actions[i] = MathUtil.Clamp01((MathUtil.FastTanh(actions[i]) + 1f) * 0.5f);
        }

        if (actions.Length > BrainChannels.Explore)
        {
            actions[BrainChannels.Explore] = MathUtil.Clamp01(actions[BrainChannels.Explore] + _genome.Temperament.NoveltySeeking * 0.05f - _genome.Temperament.HarmAvoidance * 0.04f);
        }

        if (actions.Length > BrainChannels.Bond)
        {
            actions[BrainChannels.Bond] = MathUtil.Clamp01(actions[BrainChannels.Bond] + _genome.Temperament.Sociability * 0.06f + (1f - _needs.Social) * 0.05f);
        }
    }

    private void Learn(float reward, float dt)
    {
        var plasticityScale = (Type.BaselinePlasticity + Chemicals.Acetylcholine * 0.08f + Math.Abs(reward) * 0.05f) * (0.6f + _genome.Temperament.LearningRate * 0.8f) * _development.PlasticityMultiplier * dt;
        if (plasticityScale <= 0f)
        {
            return;
        }

        var synapses = _genome.Synapses;
        for (var i = 0; i < synapses.Length; i++)
        {
            var synapse = synapses[i];
            var hebbian = _lastActivation[synapse.From] * _activation[synapse.To];
            var stressPenalty = Chemicals.Cortisol * 0.015f;
            _weights[i] = MathUtil.Clamp(_weights[i] + hebbian * reward * synapse.Plasticity * plasticityScale - stressPenalty * synapse.Plasticity, -2.5f, 2.5f);
        }
    }

    private void RecordAmbientMemory(BrainInput input, float reward)
    {
        if (_tick % 4 != 0)
        {
            return;
        }

        var threat = input.Count > BrainChannels.Threat ? input[BrainChannels.Threat] : 0f;
        var social = input.Count > BrainChannels.Social ? input[BrainChannels.Social] : 0f;
        var novelty = input.Count > BrainChannels.Novelty ? input[BrainChannels.Novelty] : 0f;
        if (Math.Abs(reward) < 0.08f && threat < 0.25f && novelty < 0.35f)
        {
            return;
        }

        _memory.Record(-1, reward, threat, social, novelty);
    }

    private SynapseDebugStats CalculateSynapseStats()
    {
        if (_weights.Length == 0)
        {
            return new SynapseDebugStats(0f, 0f, 0f, 0f);
        }

        var min = _weights[0];
        var max = _weights[0];
        var sum = 0f;
        var absSum = 0f;
        for (var i = 0; i < _weights.Length; i++)
        {
            var weight = _weights[i];
            if (weight < min) min = weight;
            if (weight > max) max = weight;
            sum += weight;
            absSum += Math.Abs(weight);
        }

        return new SynapseDebugStats(min, max, sum / _weights.Length, absSum / _weights.Length);
    }
}

}
