namespace CheapNeuroSim
{
    public sealed class BrainRuntimeState
    {
        public int Tick { get; set; }
        public NeurochemicalState Chemicals { get; set; }
        public AddictionState Addiction { get; set; }
        public NeedsState Needs { get; set; }
        public AttentionState Attention { get; set; }
        public GoalState Goal { get; set; }
        public EmotionState Emotion { get; set; }
        public DevelopmentState Development { get; set; }
        public TraumaState Trauma { get; set; }
        public float[] Activations { get; set; } = new float[0];
        public float[] LearnedWeights { get; set; } = new float[0];
        public MemoryTrace[] Memories { get; set; } = new MemoryTrace[0];
        public CultureBelief[] CultureBeliefs { get; set; } = new CultureBelief[0];
        public ReputationEntry[] ReputationEntries { get; set; } = new ReputationEntry[0];
    }
}
