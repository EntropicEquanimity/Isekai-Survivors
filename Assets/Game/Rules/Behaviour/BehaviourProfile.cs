using System;

namespace Isekai
{
    /// <summary>
    /// One weighted reason for an action: <see cref="Weight"/> times how far a <see cref="BehaviourFactor"/> has got
    /// from <see cref="From"/> (0 weight) to <see cref="To"/> (full weight), clamped. The defaults 0 to 1 make it
    /// linear in the factor. <c>From</c> may be above <c>To</c> (health from 1 down to 0 is "wounded"), and a
    /// threshold is a short span: health from 0.75 to 0.70 is "below 75%". With <c>From == To</c> it is a step.
    /// </summary>
    public readonly struct BehaviourTerm
    {
        public BehaviourTerm(BehaviourFactor factor, float weight, float from = 0f, float to = 1f)
        {
            Factor = factor;
            Weight = weight;
            From = from;
            To = to;
        }

        public BehaviourFactor Factor { get; }
        public float Weight { get; }
        public float From { get; }
        public float To { get; }

        public float Evaluate(in BehaviourContext context)
        {
            float x = context.Value(Factor);
            float t;
            if (From == To) t = x >= From ? 1f : 0f;
            else
            {
                t = (x - From) / (To - From);
                t = t < 0f ? 0f : t > 1f ? 1f : t;
            }
            return Weight * t;
        }
    }

    /// <summary>How one action scores: a base plus its terms. A disabled action never scores (a priest does not fight).</summary>
    public readonly struct ActionWeights
    {
        public ActionWeights(float baseScore, params BehaviourTerm[] terms) : this(true, baseScore, terms ?? Array.Empty<BehaviourTerm>())
        {
        }

        private ActionWeights(bool enabled, float baseScore, BehaviourTerm[] terms)
        {
            Enabled = enabled;
            Base = baseScore;
            Terms = terms;
        }

        public bool Enabled { get; }
        public float Base { get; }
        public BehaviourTerm[] Terms { get; }

        public float Score(in BehaviourContext context)
        {
            float score = Base;
            BehaviourTerm[] terms = Terms;
            for (int i = 0; i < terms.Length; i++) score += terms[i].Evaluate(context);
            return score;
        }

        internal ActionWeights Changed(in BehaviourModifier modifier)
        {
            if (modifier.Disable) return new ActionWeights(false, Base, Terms ?? Array.Empty<BehaviourTerm>());
            if (modifier.Replace) return new ActionWeights(modifier.BaseDelta, modifier.Terms);
            BehaviourTerm[] own = Terms ?? Array.Empty<BehaviourTerm>();
            BehaviourTerm[] terms = own;
            if (modifier.Terms != null && modifier.Terms.Length > 0)
            {
                terms = new BehaviourTerm[own.Length + modifier.Terms.Length];
                Array.Copy(own, terms, own.Length);
                Array.Copy(modifier.Terms, 0, terms, own.Length, modifier.Terms.Length);
            }
            return new ActionWeights(Enabled, Base + modifier.BaseDelta, terms);
        }
    }

    /// <summary>
    /// A change to one action's weights: what a trait (or a Vigilance stage, or a spell) is, as data. It adds to the base,
    /// adds terms, or switches the action off. Cowardly is
    /// <c>BehaviourModifier.Add(Flee, new BehaviourTerm(Health, 6, 0.75f, 0.7f))</c>; Berserk is <c>BehaviourModifier.Off(Flee)</c>.
    /// </summary>
    public readonly struct BehaviourModifier
    {
        private BehaviourModifier(BehaviourAction action, float baseDelta, BehaviourTerm[] terms, bool disable, bool replace = false)
        {
            Action = action;
            BaseDelta = baseDelta;
            Terms = terms;
            Disable = disable;
            Replace = replace;
        }

        public BehaviourAction Action { get; }
        public float BaseDelta { get; }
        public BehaviourTerm[] Terms { get; }
        public bool Disable { get; }
        public bool Replace { get; }

        /// <summary>Makes the unit do an action it did not (a Purifier trait), scoring <paramref name="baseScore"/> plus its terms.</summary>
        public static BehaviourModifier On(BehaviourAction action, float baseScore, params BehaviourTerm[] terms) => new(action, baseScore, terms ?? Array.Empty<BehaviourTerm>(), false, true);

        /// <summary>Adds terms to an action's score (nothing if the unit does not do the action).</summary>
        public static BehaviourModifier Add(BehaviourAction action, params BehaviourTerm[] terms) => new(action, 0f, terms, false);

        /// <summary>Adds to an action's base score (nothing if the unit does not do the action).</summary>
        public static BehaviourModifier Bias(BehaviourAction action, float delta) => new(action, delta, null, false);

        /// <summary>Switches an action off.</summary>
        public static BehaviourModifier Off(BehaviourAction action) => new(action, 0f, null, true);
    }

    /// <summary>
    /// A unit type's way of deciding: for each <see cref="BehaviourAction"/> a base score and weighted terms, plus how
    /// much the action it is already doing is favoured (so it does not flip between two close scores). Immutable: a
    /// trait makes its own copy with <see cref="With"/>, so a data change never touches the type's shared profile.
    /// Actions start disabled; <see cref="Allow"/> turns them on.
    /// </summary>
    public sealed class BehaviourProfile
    {
        public static readonly int ActionCount = Enum.GetValues(typeof(BehaviourAction)).Length;
        private readonly ActionWeights[] _weights;

        public BehaviourProfile(string name, float inertia = 0.15f) : this(name, inertia, new ActionWeights[ActionCount])
        {
        }

        private BehaviourProfile(string name, float inertia, ActionWeights[] weights)
        {
            Name = name;
            Inertia = inertia;
            _weights = weights;
        }

        public string Name { get; }

        /// <summary>The bonus the current action scores, against flip-flopping.</summary>
        public float Inertia { get; }

        public ActionWeights Of(BehaviourAction action) => _weights[(int)action];

        /// <summary>A copy that also does <paramref name="action"/>, scoring <paramref name="baseScore"/> plus its terms (replaces any earlier weights for it).</summary>
        public BehaviourProfile Allow(BehaviourAction action, float baseScore, params BehaviourTerm[] terms)
        {
            var weights = (ActionWeights[])_weights.Clone();
            weights[(int)action] = new ActionWeights(baseScore, terms);
            return new BehaviourProfile(Name, Inertia, weights);
        }

        /// <summary>A copy with the modifiers applied (a trait's weights). The original is unchanged.</summary>
        public BehaviourProfile With(params BehaviourModifier[] modifiers)
        {
            var weights = (ActionWeights[])_weights.Clone();
            foreach (BehaviourModifier m in modifiers) weights[(int)m.Action] = weights[(int)m.Action].Changed(m);
            return new BehaviourProfile(Name, Inertia, weights);
        }
    }

    /// <summary>The one place an action is chosen: every enabled action's score, the best one wins.</summary>
    public static class BehaviourScoring
    {
        /// <summary>The score of one action now (negative infinity when the profile does not do it).</summary>
        public static float Score(BehaviourProfile profile, BehaviourAction action, in BehaviourContext context)
        {
            ActionWeights weights = profile.Of(action);
            return weights.Enabled ? weights.Score(context) : float.NegativeInfinity;
        }

        /// <summary>
        /// The best action for the situation. <paramref name="current"/> gets the profile's inertia as a bonus; on a tie the
        /// earlier action in <see cref="BehaviourAction"/> wins, so the choice is the same every time. Idle when nothing is enabled.
        /// </summary>
        public static BehaviourAction Choose(BehaviourProfile profile, in BehaviourContext context, BehaviourAction current)
        {
            BehaviourAction best = BehaviourAction.Idle;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < BehaviourProfile.ActionCount; i++)
            {
                var action = (BehaviourAction)i;
                float score = Score(profile, action, context);
                if (action == current) score += profile.Inertia;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = action;
                }
            }
            return best;
        }
    }
}
