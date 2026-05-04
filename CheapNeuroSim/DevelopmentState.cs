namespace CheapNeuroSim
{
    public struct DevelopmentState
    {
        public float Age;
        public DevelopmentStage Stage;
        public float PlasticityMultiplier;
        public float ImpulseControlMultiplier;
        public float CultureUptakeMultiplier;

        public static DevelopmentState Newborn()
        {
            return new DevelopmentState
            {
                Age = 0f,
                Stage = DevelopmentStage.Juvenile,
                PlasticityMultiplier = 1.45f,
                ImpulseControlMultiplier = 0.72f,
                CultureUptakeMultiplier = 1.35f
            };
        }

        public void Tick(float dt)
        {
            Age += dt;
            if (Age < 1800f)
            {
                Stage = DevelopmentStage.Juvenile;
                PlasticityMultiplier = 1.45f;
                ImpulseControlMultiplier = 0.72f;
                CultureUptakeMultiplier = 1.35f;
            }
            else if (Age < 12000f)
            {
                Stage = DevelopmentStage.Mature;
                PlasticityMultiplier = 1.0f;
                ImpulseControlMultiplier = 1.0f;
                CultureUptakeMultiplier = 1.0f;
            }
            else
            {
                Stage = DevelopmentStage.Senescent;
                PlasticityMultiplier = 0.72f;
                ImpulseControlMultiplier = 0.92f;
                CultureUptakeMultiplier = 0.78f;
            }
        }
    }
}
