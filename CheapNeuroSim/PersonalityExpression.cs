namespace CheapNeuroSim
{
    public readonly struct PersonalityExpression
    {
        public PersonalityExpression(PersonalityStyle style, float confidence)
        {
            Style = style;
            Confidence = MathUtil.Clamp01(confidence);
        }

        public PersonalityStyle Style { get; }
        public float Confidence { get; }

        public static PersonalityExpression FromState(TemperamentProfile temperament, AddictionState addiction, TraumaState trauma, NeedsState needs)
        {
            var best = PersonalityStyle.Balanced;
            var score = 0.25f;
            Consider(PersonalityStyle.Timid, temperament.HarmAvoidance * 0.6f + trauma.Hypervigilance * 0.4f, ref best, ref score);
            Consider(PersonalityStyle.Impulsive, (1f - temperament.ImpulseControl) * 0.75f + temperament.NoveltySeeking * 0.25f, ref best, ref score);
            Consider(PersonalityStyle.Gregarious, temperament.Sociability * 0.75f + needs.Social * 0.15f, ref best, ref score);
            Consider(PersonalityStyle.Obsessive, addiction.Craving * 0.6f + addiction.Sensitization * 0.3f + temperament.LearningRate * 0.1f, ref best, ref score);
            Consider(PersonalityStyle.Suspicious, trauma.Load * 0.35f + temperament.HarmAvoidance * 0.35f + (1f - temperament.Sociability) * 0.2f, ref best, ref score);
            Consider(PersonalityStyle.Dutiful, temperament.ImpulseControl * 0.45f + temperament.LearningRate * 0.35f + temperament.HarmAvoidance * 0.1f, ref best, ref score);
            return new PersonalityExpression(best, score);
        }

        private static void Consider(PersonalityStyle style, float value, ref PersonalityStyle best, ref float score)
        {
            value = MathUtil.Clamp01(value);
            if (value > score)
            {
                best = style;
                score = value;
            }
        }
    }
}
