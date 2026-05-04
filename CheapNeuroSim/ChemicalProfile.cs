using System;

namespace CheapNeuroSim
{

public readonly struct ChemicalProfile
{
    public ChemicalProfile(
        float dopamineDrive,
        float serotoninStability,
        float norepinephrineAlertness,
        float cortisolStress,
        float oxytocinBonding,
        float acetylcholineLearning)
    {
        DopamineDrive = MathUtil.Clamp01(dopamineDrive);
        SerotoninStability = MathUtil.Clamp01(serotoninStability);
        NorepinephrineAlertness = MathUtil.Clamp01(norepinephrineAlertness);
        CortisolStress = MathUtil.Clamp01(cortisolStress);
        OxytocinBonding = MathUtil.Clamp01(oxytocinBonding);
        AcetylcholineLearning = MathUtil.Clamp01(acetylcholineLearning);
    }

    public float DopamineDrive { get; }
    public float SerotoninStability { get; }
    public float NorepinephrineAlertness { get; }
    public float CortisolStress { get; }
    public float OxytocinBonding { get; }
    public float AcetylcholineLearning { get; }

    public static ChemicalProfile CreatureDefault()
    {
        return new ChemicalProfile(
            dopamineDrive: 0.52f,
            serotoninStability: 0.58f,
            norepinephrineAlertness: 0.45f,
            cortisolStress: 0.25f,
            oxytocinBonding: 0.42f,
            acetylcholineLearning: 0.50f);
    }

    public ChemicalProfile Mutated(ref StableRandom random, MutationSettings settings)
    {
        return new ChemicalProfile(
            DopamineDrive + random.NextGaussian() * settings.ChemicalSigma,
            SerotoninStability + random.NextGaussian() * settings.ChemicalSigma,
            NorepinephrineAlertness + random.NextGaussian() * settings.ChemicalSigma,
            CortisolStress + random.NextGaussian() * settings.ChemicalSigma,
            OxytocinBonding + random.NextGaussian() * settings.ChemicalSigma,
            AcetylcholineLearning + random.NextGaussian() * settings.ChemicalSigma);
    }
}

}