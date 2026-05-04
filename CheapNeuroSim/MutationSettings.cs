namespace CheapNeuroSim
{

public sealed class MutationSettings
{
    public float WeightSigma { get; set; } = 0.10f;
    public float BiasSigma { get; set; } = 0.06f;
    public float ChemicalSigma { get; set; } = 0.04f;
    public float TemperamentSigma { get; set; } = 0.05f;
    public float RewireChance { get; set; } = 0.02f;
    public float StructuralMutationChance { get; set; } = 0.01f;
    public float ActionMutationChance { get; set; } = 0.03f;
    public float SurvivalBlend { get; set; } = 0.82f;

    public static MutationSettings Conservative() => new();
}

}
