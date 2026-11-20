namespace Isekai
{
    /// <summary>Pays out a per-second drain as whole Souls, keeping the fraction for the next tick.</summary>
    public sealed class SoulDrain
    {
        private float _carry;

        /// <summary>Whole Souls due after <paramref name="seconds"/> at <paramref name="perSecond"/>.</summary>
        public int Tick(float seconds, float perSecond)
        {
            _carry += seconds * perSecond;
            int whole = (int)_carry;
            _carry -= whole;
            return whole;
        }

        /// <summary>Forgets the fraction (a new wisp starts fresh).</summary>
        public void Reset() => _carry = 0f;
    }
}
