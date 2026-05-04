namespace CheapNeuroSim
{
    public sealed class ReputationDebugSnapshot
    {
        internal ReputationDebugSnapshot(ReputationEntry[] entries)
        {
            Entries = entries;
        }

        public ReputationEntry[] Entries { get; }
    }
}
