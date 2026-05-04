namespace CheapNeuroSim
{
    public sealed class GroupBiasDebugSnapshot
    {
        internal GroupBiasDebugSnapshot(int selfGroupId, float[] affiliationByGroup, float[] threatByGroup)
        {
            SelfGroupId = selfGroupId;
            AffiliationByGroup = affiliationByGroup;
            ThreatByGroup = threatByGroup;
        }

        public int SelfGroupId { get; }
        public float[] AffiliationByGroup { get; }
        public float[] ThreatByGroup { get; }
    }
}
