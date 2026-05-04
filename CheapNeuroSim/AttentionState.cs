namespace CheapNeuroSim
{
    public struct AttentionState
    {
        public float Hunger;
        public float Threat;
        public float Pain;
        public float Social;
        public float Novelty;
        public float AddictionCue;
        public int DominantChannel;
        public float DominantSalience;

        public void Update(BrainInput input, AddictionState addiction, NeedsState needs, TemperamentProfile temperament, float dt)
        {
            Hunger = Approach(Hunger, Read(input, BrainChannels.Hunger) + (1f - needs.Nutrition) * 0.55f, dt);
            Threat = Approach(Threat, Read(input, BrainChannels.Threat) * (0.75f + temperament.HarmAvoidance * 0.5f), dt);
            Pain = Approach(Pain, Read(input, BrainChannels.Pain) + (1f - needs.Integrity) * 0.45f, dt);
            Social = Approach(Social, Read(input, BrainChannels.Social) + (1f - needs.Social) * (0.25f + temperament.Sociability * 0.35f), dt);
            Novelty = Approach(Novelty, Read(input, BrainChannels.Novelty) * (0.6f + temperament.NoveltySeeking * 0.6f), dt);
            AddictionCue = Approach(AddictionCue, addiction.Craving * 0.7f + addiction.Withdrawal * 0.3f, dt);

            DominantChannel = BrainChannels.Hunger;
            DominantSalience = Hunger;
            SetDominant(BrainChannels.Threat, Threat);
            SetDominant(BrainChannels.Pain, Pain);
            SetDominant(BrainChannels.Social, Social);
            SetDominant(BrainChannels.Novelty, Novelty);
            SetDominant(100, AddictionCue);
        }

        public float GetSensorGain(int channel)
        {
            if (channel == DominantChannel) return 1.35f;
            return 0.82f;
        }

        private static float Read(BrainInput input, int index)
        {
            return input.Count > index ? MathUtil.Clamp01(input[index]) : 0f;
        }

        private static float Approach(float current, float target, float dt)
        {
            return MathUtil.Approach(current, MathUtil.Clamp01(target), MathUtil.Clamp01(dt * 1.8f));
        }

        private void SetDominant(int channel, float value)
        {
            if (value > DominantSalience)
            {
                DominantChannel = channel;
                DominantSalience = value;
            }
        }
    }
}
