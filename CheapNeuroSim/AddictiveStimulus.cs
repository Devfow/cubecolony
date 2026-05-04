namespace CheapNeuroSim
{
    public readonly struct AddictiveStimulus
    {
        public AddictiveStimulus(float rewardSpike, float relief, float harm, float toleranceLoad)
        {
            RewardSpike = MathUtil.Clamp01(rewardSpike);
            Relief = MathUtil.Clamp01(relief);
            Harm = MathUtil.Clamp01(harm);
            ToleranceLoad = MathUtil.Clamp01(toleranceLoad);
        }

        public float RewardSpike { get; }
        public float Relief { get; }
        public float Harm { get; }
        public float ToleranceLoad { get; }

        public static AddictiveStimulus MildReward(float intensity)
        {
            return new AddictiveStimulus(intensity * 0.35f, intensity * 0.15f, intensity * 0.04f, intensity * 0.12f);
        }

        public static AddictiveStimulus StrongReward(float intensity)
        {
            return new AddictiveStimulus(intensity * 0.85f, intensity * 0.45f, intensity * 0.18f, intensity * 0.55f);
        }
    }
}
