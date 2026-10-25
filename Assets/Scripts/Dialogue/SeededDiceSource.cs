using System;

namespace SnowyWinter.Dialogue
{
    /// <summary>
    /// Deterministic die: the value is a hash of the playthrough seed and the
    /// check id. string.GetHashCode and seeded System.Random are not
    /// guaranteed to be stable between runs, so the hash is computed here.
    /// </summary>
    public sealed class SeededDiceSource : IDiceSource
    {
        private const uint FnvOffset = 2166136261;
        private const uint FnvPrime = 16777619;

        public int Roll(int playthroughSeed, string checkId)
        {
            if (checkId == null)
            {
                throw new ArgumentNullException(nameof(checkId));
            }

            unchecked
            {
                // FNV-1a over the seed bytes, then over both bytes of every char.
                uint hash = FnvOffset;
                uint seed = (uint)playthroughSeed;
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash = (hash ^ ((seed >> shift) & 0xFF)) * FnvPrime;
                }

                foreach (char character in checkId)
                {
                    hash = (hash ^ (character & 0xFFu)) * FnvPrime;
                    hash = (hash ^ ((uint)character >> 8)) * FnvPrime;
                }

                // Murmur3 finalizer: FNV alone leaves the low bits weak, and % 6 reads them.
                hash ^= hash >> 16;
                hash *= 0x85EBCA6B;
                hash ^= hash >> 13;
                hash *= 0xC2B2AE35;
                hash ^= hash >> 16;

                return (int)(hash % DiceCheck.Sides) + 1;
            }
        }
    }
}
