namespace CheapNeuroSim
{
    public struct NeedsState
    {
        public float Energy;
        public float Nutrition;
        public float Integrity;
        public float Social;
        public float Stimulation;

        public static NeedsState Balanced()
        {
            return new NeedsState
            {
                Energy = 0.72f,
                Nutrition = 0.68f,
                Integrity = 0.92f,
                Social = 0.55f,
                Stimulation = 0.50f
            };
        }

        public void TickSensors(BrainInput input, NeedsProfile profile, float dt)
        {
            var hunger = input.Count > BrainChannels.Hunger ? input[BrainChannels.Hunger] : 0f;
            var pain = input.Count > BrainChannels.Pain ? input[BrainChannels.Pain] : 0f;
            var social = input.Count > BrainChannels.Social ? input[BrainChannels.Social] : 0f;
            var novelty = input.Count > BrainChannels.Novelty ? input[BrainChannels.Novelty] : 0f;
            var energySensor = input.Count > BrainChannels.Energy ? input[BrainChannels.Energy] : 0f;

            Energy = MathUtil.Clamp01(Energy - profile.EnergyDecay * dt + energySensor * 0.018f * dt);
            Nutrition = MathUtil.Clamp01(Nutrition - (profile.NutritionDecay + hunger * 0.010f) * dt);
            Integrity = MathUtil.Clamp01(Integrity - pain * 0.020f * dt);
            Social = MathUtil.Clamp01(Social - profile.SocialDecay * dt + social * 0.020f * dt);
            Stimulation = MathUtil.Clamp01(Stimulation - profile.StimulationDecay * dt + novelty * 0.024f * dt);
            Integrity = MathUtil.Clamp01(Integrity + profile.RepairRate * 0.1f * dt);
        }

        public void TickActions(BrainOutput output, float dt)
        {
            var move = output.Count > BrainChannels.Move ? output[BrainChannels.Move] : 0f;
            var eat = output.Count > BrainChannels.Eat ? output[BrainChannels.Eat] : 0f;
            var rest = output.Count > BrainChannels.Rest ? output[BrainChannels.Rest] : 0f;
            var explore = output.Count > BrainChannels.Explore ? output[BrainChannels.Explore] : 0f;
            var bond = output.Count > BrainChannels.Bond ? output[BrainChannels.Bond] : 0f;

            Energy = MathUtil.Clamp01(Energy - (move * 0.012f + explore * 0.008f) * dt + rest * 0.020f * dt);
            Nutrition = MathUtil.Clamp01(Nutrition + eat * 0.026f * dt);
            Social = MathUtil.Clamp01(Social + bond * 0.020f * dt);
            Stimulation = MathUtil.Clamp01(Stimulation + explore * 0.018f * dt - rest * 0.006f * dt);
        }

        public float RewardPressure()
        {
            var deficit = (1f - Energy) * 0.14f + (1f - Nutrition) * 0.16f + (1f - Integrity) * 0.22f + (1f - Social) * 0.08f + (1f - Stimulation) * 0.05f;
            return -MathUtil.Clamp(deficit, 0f, 0.45f);
        }

        public float StressPressure()
        {
            return MathUtil.Clamp01((1f - Energy) * 0.16f + (1f - Nutrition) * 0.18f + (1f - Integrity) * 0.32f + (1f - Social) * 0.08f);
        }
    }
}
