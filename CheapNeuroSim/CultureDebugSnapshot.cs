namespace CheapNeuroSim
{
    public sealed class CultureDebugSnapshot
    {
        internal CultureDebugSnapshot(CultureBelief[] beliefs)
        {
            Beliefs = beliefs;
        }

        public CultureBelief[] Beliefs { get; }
    }
}
