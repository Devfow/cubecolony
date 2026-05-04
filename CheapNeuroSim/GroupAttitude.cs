namespace CheapNeuroSim
{
    public readonly struct GroupAttitude
    {
        public GroupAttitude(float affiliation, float threat)
        {
            Affiliation = MathUtil.Clamp(affiliation, -1f, 1f);
            Threat = MathUtil.Clamp01(threat);
        }

        public float Affiliation { get; }
        public float Threat { get; }
    }
}
