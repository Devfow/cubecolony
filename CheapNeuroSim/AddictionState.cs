namespace CheapNeuroSim
{
    public struct AddictionState
    {
        public float Sensitization;
        public float Tolerance;
        public float Craving;
        public float Withdrawal;
        public float Dependence;
        public float RecentUse;

        public void Tick(ref NeurochemicalState chemicals, BrainInput input, float dt)
        {
            var cue = input.Count > BrainChannels.Novelty ? input[BrainChannels.Novelty] : 0f;
            var hunger = input.Count > BrainChannels.Hunger ? input[BrainChannels.Hunger] : 0f;

            RecentUse = MathUtil.Approach(RecentUse, 0f, dt * 0.08f);
            Craving = MathUtil.Clamp01(Craving + (Dependence * 0.018f + cue * Sensitization * 0.025f + hunger * 0.008f) * dt - RecentUse * 0.12f * dt);
            Withdrawal = MathUtil.Clamp01(Withdrawal + Dependence * (1f - RecentUse) * 0.014f * dt - RecentUse * 0.18f * dt);
            Tolerance = MathUtil.Approach(Tolerance, 0f, dt * 0.01f);

            chemicals.Dopamine = MathUtil.Clamp01(chemicals.Dopamine - Craving * 0.12f - Tolerance * 0.08f);
            chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + Withdrawal * 0.22f + Craving * 0.08f);
            chemicals.Norepinephrine = MathUtil.Clamp01(chemicals.Norepinephrine + Craving * 0.12f + Withdrawal * 0.14f);
            chemicals.Serotonin = MathUtil.Clamp01(chemicals.Serotonin - Withdrawal * 0.10f);
        }

        public void Apply(AddictiveStimulus stimulus, ref NeurochemicalState chemicals)
        {
            var rewardAfterTolerance = stimulus.RewardSpike * (1f - Tolerance * 0.65f);
            chemicals.Dopamine = MathUtil.Clamp01(chemicals.Dopamine + rewardAfterTolerance);
            chemicals.Cortisol = MathUtil.Clamp01(chemicals.Cortisol + stimulus.Harm - stimulus.Relief * 0.5f);
            chemicals.Norepinephrine = MathUtil.Clamp01(chemicals.Norepinephrine + stimulus.RewardSpike * 0.12f + stimulus.Harm * 0.2f);

            RecentUse = MathUtil.Clamp01(RecentUse + stimulus.RewardSpike + stimulus.Relief * 0.5f);
            Craving = MathUtil.Clamp01(Craving - stimulus.Relief * 0.55f + Sensitization * 0.04f);
            Withdrawal = MathUtil.Clamp01(Withdrawal - stimulus.Relief * 0.75f);
            Tolerance = MathUtil.Clamp01(Tolerance + stimulus.ToleranceLoad * 0.16f);
            Dependence = MathUtil.Clamp01(Dependence + stimulus.ToleranceLoad * 0.08f + stimulus.Relief * 0.03f);
            Sensitization = MathUtil.Clamp01(Sensitization + stimulus.RewardSpike * 0.035f);
        }
    }
}
