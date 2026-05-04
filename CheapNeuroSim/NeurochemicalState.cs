namespace CheapNeuroSim
{

public struct NeurochemicalState
{
    public float Dopamine;
    public float Serotonin;
    public float Norepinephrine;
    public float Cortisol;
    public float Oxytocin;
    public float Acetylcholine;

    public static NeurochemicalState FromProfile(ChemicalProfile profile)
    {
        return new NeurochemicalState
        {
            Dopamine = profile.DopamineDrive,
            Serotonin = profile.SerotoninStability,
            Norepinephrine = profile.NorepinephrineAlertness,
            Cortisol = profile.CortisolStress,
            Oxytocin = profile.OxytocinBonding,
            Acetylcholine = profile.AcetylcholineLearning
        };
    }

    public void Tick(ChemicalProfile profile, BrainInput input, float reward, float dt)
    {
        var hunger = input.Count > BrainChannels.Hunger ? input[BrainChannels.Hunger] : 0f;
        var threat = input.Count > BrainChannels.Threat ? input[BrainChannels.Threat] : 0f;
        var pain = input.Count > BrainChannels.Pain ? input[BrainChannels.Pain] : 0f;
        var social = input.Count > BrainChannels.Social ? input[BrainChannels.Social] : 0f;
        var novelty = input.Count > BrainChannels.Novelty ? input[BrainChannels.Novelty] : 0f;
        var energy = input.Count > BrainChannels.Energy ? input[BrainChannels.Energy] : 0f;

        Dopamine = MathUtil.Approach(Dopamine, profile.DopamineDrive + reward * 0.45f + novelty * 0.16f - pain * 0.18f, dt * 2.5f);
        Serotonin = MathUtil.Approach(Serotonin, profile.SerotoninStability + energy * 0.16f - threat * 0.18f, dt * 1.6f);
        Norepinephrine = MathUtil.Approach(Norepinephrine, profile.NorepinephrineAlertness + threat * 0.45f + hunger * 0.12f, dt * 4.0f);
        Cortisol = MathUtil.Approach(Cortisol, profile.CortisolStress + threat * 0.38f + pain * 0.32f - reward * 0.16f, dt * 1.2f);
        Oxytocin = MathUtil.Approach(Oxytocin, profile.OxytocinBonding + social * 0.35f - threat * 0.12f, dt * 1.4f);
        Acetylcholine = MathUtil.Approach(Acetylcholine, profile.AcetylcholineLearning + novelty * 0.28f + reward * 0.12f, dt * 2.2f);

        Dopamine = MathUtil.Clamp01(Dopamine);
        Serotonin = MathUtil.Clamp01(Serotonin);
        Norepinephrine = MathUtil.Clamp01(Norepinephrine);
        Cortisol = MathUtil.Clamp01(Cortisol);
        Oxytocin = MathUtil.Clamp01(Oxytocin);
        Acetylcholine = MathUtil.Clamp01(Acetylcholine);
    }
}

}