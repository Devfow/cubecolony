namespace CheapNeuroSim
{
    public struct EmotionState
    {
        public EmotionKind Primary;
        public float Intensity;
        public float Fear;
        public float Curiosity;
        public float Loneliness;
        public float Craving;
        public float Affection;
        public float Anger;
        public float Exhaustion;
        public float Hurt;

        public void Update(NeurochemicalState chemicals, AddictionState addiction, NeedsState needs, AttentionState attention, TraumaState trauma)
        {
            Fear = MathUtil.Clamp01(chemicals.Cortisol * 0.45f + chemicals.Norepinephrine * 0.25f + attention.Threat * 0.35f + trauma.Hypervigilance * 0.25f);
            Curiosity = MathUtil.Clamp01(chemicals.Dopamine * 0.25f + attention.Novelty * 0.55f);
            Loneliness = MathUtil.Clamp01((1f - needs.Social) * 0.75f);
            Craving = MathUtil.Clamp01(addiction.Craving * 0.75f + addiction.Withdrawal * 0.35f);
            Affection = MathUtil.Clamp01(chemicals.Oxytocin * 0.7f + needs.Social * 0.1f);
            Anger = MathUtil.Clamp01(chemicals.Norepinephrine * 0.25f + attention.Pain * 0.35f + trauma.TriggerLoad * 0.25f);
            Exhaustion = MathUtil.Clamp01((1f - needs.Energy) * 0.75f + (1f - needs.Nutrition) * 0.25f);
            Hurt = MathUtil.Clamp01((1f - needs.Integrity) * 0.8f + attention.Pain * 0.35f);

            Primary = EmotionKind.Calm;
            Intensity = MathUtil.Clamp01(chemicals.Serotonin * 0.25f);
            Consider(EmotionKind.Afraid, Fear);
            Consider(EmotionKind.Curious, Curiosity);
            Consider(EmotionKind.Lonely, Loneliness);
            Consider(EmotionKind.Craving, Craving);
            Consider(EmotionKind.Affectionate, Affection);
            Consider(EmotionKind.Angry, Anger);
            Consider(EmotionKind.Exhausted, Exhaustion);
            Consider(EmotionKind.Hurt, Hurt);
        }

        private void Consider(EmotionKind kind, float value)
        {
            if (value > Intensity)
            {
                Primary = kind;
                Intensity = value;
            }
        }
    }
}
