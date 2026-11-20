namespace Isekai
{
    /// <summary>
    /// How each demon decides, as <see cref="BehaviourProfile"/>s on the shared behaviours (the same scoring as mortals
    /// and wildlife). A demon's Portal order decides where it stands and how far it chases (<see cref="DemonPosts"/>);
    /// these weights decide what it does there:
    /// Fight when something hostile is in reach, ReturnHome when it is away from its post (urgently past its leash),
    /// Work (a bone demon: spend a pile) when calm, Idle otherwise. Traits change a copy with
    /// <see cref="BehaviourProfile.With"/>; none flee for now.
    /// </summary>
    public static class DemonProfiles
    {
        private static readonly BehaviourProfile s_imp = Fighter("Imp");
        private static readonly BehaviourProfile s_skeleton = Fighter("Skeleton");

        // A bone demon fights when something is near, and works the piles when it is calm; a pile is worth a walk to the edge of its leash, but not past a fight.
        private static readonly BehaviourProfile s_boneDemon = Fighter("Bone Demon")
            .Allow(BehaviourAction.Work, 0.95f);

        public static BehaviourProfile For(DemonType type) => type switch
        {
            DemonType.BoneDemon => s_boneDemon,
            DemonType.Skeleton => s_skeleton,
            _ => s_imp,
        };

        private static BehaviourProfile Fighter(string name) => new BehaviourProfile(name)
            .Allow(BehaviourAction.Idle, 0.1f)
            .Allow(BehaviourAction.Fight, 0f,
                new BehaviourTerm(BehaviourFactor.Threat, 1f),
                new BehaviourTerm(BehaviourFactor.ThreatClose, 0.3f))
            .Allow(BehaviourAction.ReturnHome, 0f,
                new BehaviourTerm(BehaviourFactor.Far, 0.9f),                 // the further away the more it wants back
                new BehaviourTerm(BehaviourFactor.Far, 2f, 1f, 1.2f));         // past its leash it goes back before anything
    }
}
