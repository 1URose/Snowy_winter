using System;
using System.Linq;
using NUnit.Framework;
using SnowyWinter.Dialogue;

namespace SnowyWinter.Tests
{
    public sealed class SeededDiceSourceTests
    {
        private readonly SeededDiceSource dice = new SeededDiceSource();

        [Test]
        public void SameSeedAndCheckId_GiveSameValue()
        {
            Assert.That(dice.Roll(12345, "lock-pick"), Is.EqualTo(dice.Roll(12345, "lock-pick")));
            Assert.That(new SeededDiceSource().Roll(-7, "замок"), Is.EqualTo(dice.Roll(-7, "замок")));
        }

        [Test]
        public void KnownInputs_GiveFixedValues()
        {
            // Pins the hash: if it changes, existing saves would roll differently.
            int[] values = Enumerable.Range(0, 10).Select(index => dice.Roll(2026, "check-" + index)).ToArray();

            Assert.That(values, Is.EqualTo(new[] { 4, 6, 2, 1, 4, 2, 5, 2, 5, 3 }));
        }

        [Test]
        public void DifferentCheckIds_AreIndependentAndCoverAllFaces()
        {
            int[] faceCounts = new int[DiceCheck.Sides + 1];
            for (int index = 0; index < 6000; index++)
            {
                faceCounts[dice.Roll(42, "check-" + index)]++;
            }

            Assert.That(faceCounts[0], Is.Zero);
            for (int face = 1; face <= DiceCheck.Sides; face++)
            {
                // 1000 expected per face; 900..1100 is about three standard deviations.
                Assert.That(faceCounts[face], Is.InRange(900, 1100), "face " + face);
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentRolls()
        {
            int[] first = Enumerable.Range(0, 50).Select(index => dice.Roll(1, "check-" + index)).ToArray();
            int[] second = Enumerable.Range(0, 50).Select(index => dice.Roll(2, "check-" + index)).ToArray();

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void NullCheckId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => dice.Roll(1, null));
        }
    }
}
