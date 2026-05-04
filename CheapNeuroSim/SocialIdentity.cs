namespace CheapNeuroSim
{
    public readonly struct SocialIdentity
    {
        public SocialIdentity(int groupId, float visibleMarkerStrength = 1f)
        {
            GroupId = groupId < 0 ? 0 : groupId;
            VisibleMarkerStrength = MathUtil.Clamp01(visibleMarkerStrength);
        }

        public int GroupId { get; }
        public float VisibleMarkerStrength { get; }
    }
}
