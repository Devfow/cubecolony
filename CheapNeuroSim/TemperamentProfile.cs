namespace CheapNeuroSim
{
    public readonly struct TemperamentProfile
    {
        public TemperamentProfile(
            float noveltySeeking,
            float harmAvoidance,
            float sociability,
            float aggression,
            float learningRate,
            float impulseControl)
        {
            NoveltySeeking = MathUtil.Clamp01(noveltySeeking);
            HarmAvoidance = MathUtil.Clamp01(harmAvoidance);
            Sociability = MathUtil.Clamp01(sociability);
            Aggression = MathUtil.Clamp01(aggression);
            LearningRate = MathUtil.Clamp01(learningRate);
            ImpulseControl = MathUtil.Clamp01(impulseControl);
        }

        public float NoveltySeeking { get; }
        public float HarmAvoidance { get; }
        public float Sociability { get; }
        public float Aggression { get; }
        public float LearningRate { get; }
        public float ImpulseControl { get; }

        public static TemperamentProfile CreatureDefault()
        {
            return new TemperamentProfile(0.52f, 0.46f, 0.50f, 0.18f, 0.50f, 0.56f);
        }

        public TemperamentProfile Mutated(ref StableRandom random, MutationSettings settings)
        {
            var sigma = settings.TemperamentSigma;
            return new TemperamentProfile(
                NoveltySeeking + random.NextGaussian() * sigma,
                HarmAvoidance + random.NextGaussian() * sigma,
                Sociability + random.NextGaussian() * sigma,
                Aggression + random.NextGaussian() * sigma,
                LearningRate + random.NextGaussian() * sigma,
                ImpulseControl + random.NextGaussian() * sigma);
        }
    }
}
