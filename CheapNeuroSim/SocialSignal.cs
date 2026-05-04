namespace CheapNeuroSim
{

public readonly struct SocialSignal
{
    public SocialSignal(float intensity, float affiliation, float stress, float valence)
    {
        Intensity = MathUtil.Clamp01(intensity);
        Affiliation = MathUtil.Clamp01(affiliation);
        Stress = MathUtil.Clamp01(stress);
        Valence = MathUtil.Clamp01(valence);
    }

    public float Intensity { get; }
    public float Affiliation { get; }
    public float Stress { get; }
    public float Valence { get; }
}

}