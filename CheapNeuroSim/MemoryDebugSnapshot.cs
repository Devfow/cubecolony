namespace CheapNeuroSim
{
    public sealed class MemoryDebugSnapshot
    {
        internal MemoryDebugSnapshot(MemoryTrace[] traces, float averageValence, float averageThreat)
        {
            Traces = traces;
            AverageValence = averageValence;
            AverageThreat = averageThreat;
        }

        public MemoryTrace[] Traces { get; }
        public float AverageValence { get; }
        public float AverageThreat { get; }
    }
}
