namespace CheapNeuroSim
{
    public struct GoalState
    {
        public GoalKind Primary;
        public float Strength;
        public int Age;

        public void Update(BrainOutput output, NeedsState needs, AddictionState addiction, AttentionState attention, float dt)
        {
            var best = GoalKind.None;
            var bestValue = 0f;
            Consider(GoalKind.Eat, Read(output, BrainChannels.Eat) + (1f - needs.Nutrition) * 0.55f, ref best, ref bestValue);
            Consider(GoalKind.Flee, Read(output, BrainChannels.Flee) + attention.Threat * 0.65f + attention.Pain * 0.35f, ref best, ref bestValue);
            Consider(GoalKind.Rest, Read(output, BrainChannels.Rest) + (1f - needs.Energy) * 0.55f, ref best, ref bestValue);
            Consider(GoalKind.Explore, Read(output, BrainChannels.Explore) + (1f - needs.Stimulation) * 0.35f, ref best, ref bestValue);
            Consider(GoalKind.Bond, Read(output, BrainChannels.Bond) + (1f - needs.Social) * 0.45f, ref best, ref bestValue);
            Consider(GoalKind.SeekAddictiveStimulus, addiction.Craving * 0.8f + addiction.Withdrawal * 0.35f, ref best, ref bestValue);
            Consider(GoalKind.Repair, (1f - needs.Integrity) * 0.9f, ref best, ref bestValue);

            if (best == Primary)
            {
                Age++;
                Strength = MathUtil.Approach(Strength, bestValue, MathUtil.Clamp01(dt * 0.8f));
            }
            else if (bestValue > Strength * 0.82f || Age > 80)
            {
                Primary = best;
                Strength = bestValue;
                Age = 0;
            }
            else
            {
                Age++;
                Strength = MathUtil.Approach(Strength, bestValue, MathUtil.Clamp01(dt * 0.25f));
            }
        }

        private static float Read(BrainOutput output, int index)
        {
            return output.Count > index ? output[index] : 0f;
        }

        private static void Consider(GoalKind goal, float value, ref GoalKind best, ref float bestValue)
        {
            value = MathUtil.Clamp01(value);
            if (value > bestValue)
            {
                best = goal;
                bestValue = value;
            }
        }
    }
}
