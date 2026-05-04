namespace CheapNeuroSim
{
    public readonly struct SynapseDebugStats
    {
        public SynapseDebugStats(float minimumWeight, float maximumWeight, float averageWeight, float averageAbsoluteWeight)
        {
            MinimumWeight = minimumWeight;
            MaximumWeight = maximumWeight;
            AverageWeight = averageWeight;
            AverageAbsoluteWeight = averageAbsoluteWeight;
        }

        public float MinimumWeight { get; }
        public float MaximumWeight { get; }
        public float AverageWeight { get; }
        public float AverageAbsoluteWeight { get; }
    }
}
