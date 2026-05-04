namespace CheapNeuroSim
{
    public readonly struct IndividualIdentity
    {
        public IndividualIdentity(int id, SocialIdentity socialIdentity)
        {
            Id = id;
            SocialIdentity = socialIdentity;
        }

        public int Id { get; }
        public SocialIdentity SocialIdentity { get; }
    }
}
