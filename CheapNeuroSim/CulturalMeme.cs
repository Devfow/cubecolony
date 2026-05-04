namespace CheapNeuroSim
{
    public readonly struct CulturalMeme
    {
        public CulturalMeme(int topicId, float valence, float confidence, float virulence = 0.5f, float mutationRate = 0.02f)
        {
            TopicId = topicId;
            Valence = MathUtil.Clamp(valence, -1f, 1f);
            Confidence = MathUtil.Clamp01(confidence);
            Virulence = MathUtil.Clamp01(virulence);
            MutationRate = MathUtil.Clamp01(mutationRate);
        }

        public int TopicId { get; }
        public float Valence { get; }
        public float Confidence { get; }
        public float Virulence { get; }
        public float MutationRate { get; }

        public CulturalMeme Mutated(uint seed)
        {
            var random = new StableRandom(seed);
            if (!random.Chance(MutationRate))
            {
                return this;
            }

            return new CulturalMeme(
                TopicId,
                Valence + random.NextGaussian() * 0.12f,
                Confidence + random.NextGaussian() * 0.08f,
                Virulence + random.NextGaussian() * 0.08f,
                MutationRate);
        }
    }
}
