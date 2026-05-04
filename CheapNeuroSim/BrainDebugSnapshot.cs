using System;

namespace CheapNeuroSim
{
    public sealed class BrainDebugSnapshot
    {
        internal BrainDebugSnapshot(
            string brainTypeId,
            int tick,
            NeurochemicalState chemicals,
            AddictionState addiction,
            NeedsState needs,
            AttentionState attention,
            GoalState goal,
            EmotionState emotion,
            DevelopmentState development,
            TraumaState trauma,
            TemperamentProfile temperament,
            PersonalityExpression personality,
            float[] activations,
            float[] actions,
            SynapseDebugStats synapses,
            GroupBiasDebugSnapshot socialBias,
            MemoryDebugSnapshot memory,
            CultureDebugSnapshot culture,
            ReputationDebugSnapshot reputation)
        {
            BrainTypeId = brainTypeId;
            Tick = tick;
            Chemicals = chemicals;
            Addiction = addiction;
            Needs = needs;
            Attention = attention;
            Goal = goal;
            Emotion = emotion;
            Development = development;
            Trauma = trauma;
            Temperament = temperament;
            Personality = personality;
            Activations = activations;
            Actions = actions;
            Synapses = synapses;
            SocialBias = socialBias;
            Memory = memory;
            Culture = culture;
            Reputation = reputation;
        }

        public string BrainTypeId { get; }
        public int Tick { get; }
        public NeurochemicalState Chemicals { get; }
        public AddictionState Addiction { get; }
        public NeedsState Needs { get; }
        public AttentionState Attention { get; }
        public GoalState Goal { get; }
        public EmotionState Emotion { get; }
        public DevelopmentState Development { get; }
        public TraumaState Trauma { get; }
        public TemperamentProfile Temperament { get; }
        public PersonalityExpression Personality { get; }
        public float[] Activations { get; }
        public float[] Actions { get; }
        public SynapseDebugStats Synapses { get; }
        public GroupBiasDebugSnapshot SocialBias { get; }
        public MemoryDebugSnapshot Memory { get; }
        public CultureDebugSnapshot Culture { get; }
        public ReputationDebugSnapshot Reputation { get; }

        public int DominantActionIndex
        {
            get
            {
                var bestIndex = 0;
                var bestValue = Actions.Length == 0 ? 0f : Actions[0];
                for (var i = 1; i < Actions.Length; i++)
                {
                    if (Actions[i] > bestValue)
                    {
                        bestIndex = i;
                        bestValue = Actions[i];
                    }
                }

                return bestIndex;
            }
        }
    }
}
