namespace CheapNeuroSim
{
    public readonly struct NeedsProfile
    {
        public NeedsProfile(float energyDecay, float nutritionDecay, float socialDecay, float stimulationDecay, float repairRate)
        {
            EnergyDecay = MathUtil.Clamp(energyDecay, 0f, 0.1f);
            NutritionDecay = MathUtil.Clamp(nutritionDecay, 0f, 0.1f);
            SocialDecay = MathUtil.Clamp(socialDecay, 0f, 0.1f);
            StimulationDecay = MathUtil.Clamp(stimulationDecay, 0f, 0.1f);
            RepairRate = MathUtil.Clamp(repairRate, 0f, 0.1f);
        }

        public float EnergyDecay { get; }
        public float NutritionDecay { get; }
        public float SocialDecay { get; }
        public float StimulationDecay { get; }
        public float RepairRate { get; }

        public static NeedsProfile CreatureDefault()
        {
            return new NeedsProfile(0.006f, 0.004f, 0.004f, 0.006f, 0.008f);
        }
    }
}
