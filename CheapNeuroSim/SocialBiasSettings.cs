namespace CheapNeuroSim
{
    public sealed class SocialBiasSettings
    {
        public float InGroupAffiliation { get; set; } = 0.08f;
        public float OutGroupSuspicion { get; set; } = 0.02f;
        public float LearningRate { get; set; } = 0.08f;
        public float ExtinctionRate { get; set; } = 0.01f;
        public float Generalization { get; set; } = 0.12f;

        public static SocialBiasSettings LowBias() => new SocialBiasSettings
        {
            InGroupAffiliation = 0.03f,
            OutGroupSuspicion = 0.0f,
            LearningRate = 0.04f,
            ExtinctionRate = 0.03f,
            Generalization = 0.04f
        };

        public static SocialBiasSettings PrejudicedCulture() => new SocialBiasSettings
        {
            InGroupAffiliation = 0.16f,
            OutGroupSuspicion = 0.16f,
            LearningRate = 0.11f,
            ExtinctionRate = 0.004f,
            Generalization = 0.28f
        };
    }
}
