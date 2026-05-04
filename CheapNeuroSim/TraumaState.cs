namespace CheapNeuroSim
{
    public struct TraumaState
    {
        public float Load;
        public float Hypervigilance;
        public float TriggerLoad;
        public float Recovery;

        public void RegisterStress(float threat, float pain, float helplessness, float dt)
        {
            var impact = MathUtil.Clamp01(threat * 0.4f + pain * 0.35f + helplessness * 0.25f);
            Load = MathUtil.Clamp01(Load + impact * 0.035f * dt);
            Hypervigilance = MathUtil.Clamp01(Hypervigilance + impact * 0.045f * dt);
            TriggerLoad = MathUtil.Clamp01(TriggerLoad + impact * 0.060f * dt);
        }

        public void Recover(float safety, float socialSupport, float dt)
        {
            var healing = MathUtil.Clamp01(safety * 0.55f + socialSupport * 0.45f);
            Recovery = MathUtil.Clamp01(Recovery + healing * 0.018f * dt);
            Load = MathUtil.Clamp01(Load - healing * 0.012f * dt);
            Hypervigilance = MathUtil.Clamp01(Hypervigilance - healing * 0.018f * dt);
            TriggerLoad = MathUtil.Clamp01(TriggerLoad - (0.008f + healing * 0.025f) * dt);
        }
    }
}
