using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowyWinter.Dialogue;
using SnowyWinter.SaveSystem;

namespace SnowyWinter.Tests
{
    public sealed class DiceCheckTests
    {
        private GameStateManager stateManager;
        private FakeDice dice;
        private DiceCheck check;

        [SetUp]
        public void SetUp()
        {
            stateManager = new GameStateManager(new GameStateData { playthroughSeed = 42 });
            dice = new FakeDice();
            check = new DiceCheck(stateManager, dice);
        }

        [Test]
        public void Constructor_RejectsNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new DiceCheck(null, dice));
            Assert.Throws<ArgumentNullException>(() => new DiceCheck(stateManager, null));
        }

        [Test]
        public void NewCheck_IsClosed()
        {
            Assert.That(check.IsOpen, Is.False);
            Assert.That(check.CheckId, Is.Empty);
        }

        [Test]
        public void Open_ExposesCheckIdAndRaisesOpened()
        {
            int openedCount = 0;
            check.Opened += () => openedCount++;

            check.Open("lock-pick");

            Assert.That(openedCount, Is.EqualTo(1));
            Assert.That(check.IsOpen, Is.True);
            Assert.That(check.CheckId, Is.EqualTo("lock-pick"));
            Assert.That(dice.Calls, Is.Zero);
        }

        [Test]
        public void Open_RejectsEmptyCheckId()
        {
            Assert.Throws<ArgumentException>(() => check.Open(null));
            Assert.Throws<ArgumentException>(() => check.Open(""));
            Assert.That(check.IsOpen, Is.False);
        }

        [Test]
        public void Open_WhileOpen_Throws()
        {
            check.Open("lock-pick");

            Assert.Throws<InvalidOperationException>(() => check.Open("climb"));
            Assert.That(check.CheckId, Is.EqualTo("lock-pick"));
        }

        [TestCase(1, "fail")]
        [TestCase(3, "fail")]
        [TestCase(4, "success")]
        [TestCase(6, "success")]
        public void Roll_RecordsValueAndOutcomeAndMarksStateDirty(int value, string expectedOutcome)
        {
            dice.Value = value;
            check.Open("lock-pick");

            check.Roll();

            CheckResultData saved = stateManager.Current.checkResults.Single();
            Assert.That(saved.checkId, Is.EqualTo("lock-pick"));
            Assert.That(saved.value, Is.EqualTo(value));
            Assert.That(saved.outcomeId, Is.EqualTo(expectedOutcome));
            Assert.That(stateManager.IsDirty, Is.True);
        }

        [Test]
        public void Roll_AsksDiceWithPlaythroughSeedAndCheckId()
        {
            check.Open("lock-pick");

            check.Roll();

            Assert.That(dice.LastSeed, Is.EqualTo(42));
            Assert.That(dice.LastCheckId, Is.EqualTo("lock-pick"));
        }

        [Test]
        public void Roll_ClosesCheckAndReportsStoredResult()
        {
            List<CheckResultData> rolled = new List<CheckResultData>();
            check.Rolled += rolled.Add;
            check.Open("lock-pick");

            check.Roll();

            Assert.That(rolled.Single(), Is.SameAs(stateManager.Current.checkResults.Single()));
            Assert.That(check.IsOpen, Is.False);
            Assert.That(check.CheckId, Is.Empty);
        }

        [Test]
        public void Roll_StateIsUpdatedBeforeRolled()
        {
            int savedResults = -1;
            bool dirty = false;
            bool open = true;
            check.Rolled += _ =>
            {
                savedResults = stateManager.Current.checkResults.Count;
                dirty = stateManager.IsDirty;
                open = check.IsOpen;
            };
            check.Open("lock-pick");

            check.Roll();

            Assert.That(savedResults, Is.EqualTo(1));
            Assert.That(dirty, Is.True);
            Assert.That(open, Is.False);
        }

        [Test]
        public void Roll_WithoutOpen_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => check.Roll());
            Assert.That(stateManager.Current.checkResults, Is.Empty);
            Assert.That(dice.Calls, Is.Zero);
        }

        [Test]
        public void Roll_TwiceForOneOpen_SecondCallThrows()
        {
            check.Open("lock-pick");
            check.Roll();

            Assert.Throws<InvalidOperationException>(() => check.Roll());
            Assert.That(stateManager.Current.checkResults.Count, Is.EqualTo(1));
            Assert.That(dice.Calls, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void Roll_DiceOutOfRange_ThrowsAndChangesNothing(int value)
        {
            dice.Value = value;
            check.Open("lock-pick");

            Assert.Throws<InvalidOperationException>(() => check.Roll());
            Assert.That(check.IsOpen, Is.True);
            Assert.That(stateManager.Current.checkResults, Is.Empty);
            Assert.That(stateManager.IsDirty, Is.False);
        }

        [Test]
        public void Open_AlreadyRolledCheck_ReturnsStoredResultWithoutNewRoll()
        {
            dice.Value = 2;
            check.Open("lock-pick");
            check.Roll();
            CheckResultData first = stateManager.Current.checkResults.Single();
            stateManager.MarkClean();

            int openedCount = 0;
            List<CheckResultData> rolled = new List<CheckResultData>();
            check.Opened += () => openedCount++;
            check.Rolled += rolled.Add;
            dice.Value = 6;

            check.Open("lock-pick");

            Assert.That(rolled.Single(), Is.SameAs(first));
            Assert.That(first.value, Is.EqualTo(2));
            Assert.That(openedCount, Is.Zero);
            Assert.That(check.IsOpen, Is.False);
            Assert.That(dice.Calls, Is.EqualTo(1));
            Assert.That(stateManager.Current.checkResults.Count, Is.EqualTo(1));
            Assert.That(stateManager.IsDirty, Is.False);
        }

        [Test]
        public void Open_CheckRolledInLoadedSave_ReturnsStoredResult()
        {
            CheckResultData loaded = new CheckResultData { checkId = "lock-pick", value = 5, outcomeId = "success" };
            stateManager.Replace(new GameStateData { checkResults = { loaded } });
            List<CheckResultData> rolled = new List<CheckResultData>();
            check.Rolled += rolled.Add;

            check.Open("lock-pick");

            Assert.That(rolled.Single(), Is.SameAs(loaded));
            Assert.That(dice.Calls, Is.Zero);
        }

        [Test]
        public void RolledHandler_CanOpenNextCheck()
        {
            check.Rolled += result =>
            {
                if (result.checkId == "lock-pick")
                {
                    check.Open("climb");
                }
            };
            check.Open("lock-pick");

            check.Roll();

            Assert.That(check.IsOpen, Is.True);
            Assert.That(check.CheckId, Is.EqualTo("climb"));
        }

        [Test]
        public void ReloadBeforeRoll_GivesTheSameValue()
        {
            // Same seed as in the save: rolling, reloading and rolling again must not change the value.
            int RollOnce()
            {
                GameStateManager manager = new GameStateManager(new GameStateData { playthroughSeed = 777 });
                DiceCheck freshCheck = new DiceCheck(manager, new SeededDiceSource());
                freshCheck.Open("lock-pick");
                freshCheck.Roll();
                return manager.Current.checkResults.Single().value;
            }

            Assert.That(RollOnce(), Is.EqualTo(RollOnce()));
        }

        private sealed class FakeDice : IDiceSource
        {
            public int Value = 1;
            public int Calls;
            public int LastSeed;
            public string LastCheckId;

            public int Roll(int playthroughSeed, string checkId)
            {
                Calls++;
                LastSeed = playthroughSeed;
                LastCheckId = checkId;
                return Value;
            }
        }
    }
}
