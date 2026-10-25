using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowyWinter.Dialogue;
using SnowyWinter.SaveSystem;

namespace SnowyWinter.Tests
{
    public sealed class ChoiceMenuTests
    {
        private GameStateManager stateManager;
        private ChoiceMenu menu;

        [SetUp]
        public void SetUp()
        {
            stateManager = new GameStateManager(new GameStateData { checkpointId = "cp-forest" });
            menu = new ChoiceMenu(stateManager);
        }

        [Test]
        public void Constructor_RejectsNullStateManager()
        {
            Assert.Throws<ArgumentNullException>(() => new ChoiceMenu(null));
        }

        [Test]
        public void NewMenu_IsClosedAndEmpty()
        {
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(menu.ChoiceId, Is.Empty);
            Assert.That(menu.Options, Is.Empty);
        }

        [Test]
        public void Open_ExposesOptionsInOrderAndRaisesOpened()
        {
            int openedCount = 0;
            menu.Opened += () => openedCount++;

            menu.Open("q-wolf", Options("ask", "threaten", "leave"));

            Assert.That(openedCount, Is.EqualTo(1));
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(menu.ChoiceId, Is.EqualTo("q-wolf"));
            CollectionAssert.AreEqual(new[] { "ask", "threaten", "leave" }, menu.Options.Select(o => o.Id));
            CollectionAssert.AreEqual(new[] { "ask.text", "threaten.text", "leave.text" }, menu.Options.Select(o => o.TextKey));
        }

        [Test]
        public void Open_CopiesOptions()
        {
            List<ChoiceOption> source = Options("ask", "leave").ToList();
            menu.Open("q-wolf", source);

            source.Clear();

            Assert.That(menu.Options.Count, Is.EqualTo(2));
        }

        [Test]
        public void Open_WhileOpen_Throws()
        {
            menu.Open("q-wolf", Options("ask"));

            Assert.Throws<InvalidOperationException>(() => menu.Open("q-bear", Options("run")));
            Assert.That(menu.ChoiceId, Is.EqualTo("q-wolf"));
        }

        [Test]
        public void Open_RejectsEmptyChoiceId()
        {
            Assert.Throws<ArgumentException>(() => menu.Open(null, Options("ask")));
            Assert.Throws<ArgumentException>(() => menu.Open("", Options("ask")));
        }

        [Test]
        public void Open_RejectsNullOrEmptyOptions()
        {
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", null));
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", new ChoiceOption[0]));
        }

        [Test]
        public void Open_RejectsNullOptionOrEmptyOptionId()
        {
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", new ChoiceOption[] { null }));
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", new[] { new ChoiceOption("", "key") }));
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", new[] { new ChoiceOption(null, "key") }));
        }

        [Test]
        public void Open_RejectsDuplicateOptionIds()
        {
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", Options("ask", "ask")));
        }

        [Test]
        public void Open_InvalidInput_LeavesMenuClosed()
        {
            Assert.Throws<ArgumentException>(() => menu.Open("q-wolf", Options("ask", "ask")));

            Assert.That(menu.IsOpen, Is.False);
            Assert.That(menu.Options, Is.Empty);
        }

        [Test]
        public void Choose_RecordsChoiceWithCheckpointAndMarksStateDirty()
        {
            menu.Open("q-wolf", Options("ask", "leave"));

            menu.Choose("leave");

            ChoiceData saved = stateManager.Current.choices.Single();
            Assert.That(saved.choiceId, Is.EqualTo("q-wolf"));
            Assert.That(saved.optionId, Is.EqualTo("leave"));
            Assert.That(saved.selectedAtCheckpointId, Is.EqualTo("cp-forest"));
            Assert.That(stateManager.IsDirty, Is.True);
        }

        [Test]
        public void Choose_ClosesMenuAndReportsChosenOption()
        {
            List<string> chosen = new List<string>();
            menu.OptionChosen += option => chosen.Add(option.Id);
            menu.Open("q-wolf", Options("ask", "leave"));

            menu.Choose("ask");

            CollectionAssert.AreEqual(new[] { "ask" }, chosen);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(menu.ChoiceId, Is.Empty);
            Assert.That(menu.Options, Is.Empty);
        }

        [Test]
        public void Choose_SameChoiceAgain_ReplacesPreviousAnswer()
        {
            menu.Open("q-wolf", Options("ask", "leave"));
            menu.Choose("ask");
            menu.Open("q-wolf", Options("ask", "leave"));
            menu.Choose("leave");

            ChoiceData saved = stateManager.Current.choices.Single();
            Assert.That(saved.optionId, Is.EqualTo("leave"));
        }

        [Test]
        public void Choose_KeepsAnswersOfOtherChoices()
        {
            menu.Open("q-wolf", Options("ask"));
            menu.Choose("ask");
            menu.Open("q-bear", Options("run"));
            menu.Choose("run");

            CollectionAssert.AreEquivalent(
                new[] { "q-wolf:ask", "q-bear:run" },
                stateManager.Current.choices.Select(c => c.choiceId + ":" + c.optionId));
        }

        [Test]
        public void OptionChosenHandler_CanOpenNextChoice()
        {
            menu.OptionChosen += _ => menu.Open("q-next", Options("go"));
            menu.Open("q-wolf", Options("ask"));

            menu.Choose("ask");

            Assert.That(menu.IsOpen, Is.True);
            Assert.That(menu.ChoiceId, Is.EqualTo("q-next"));
            Assert.That(stateManager.Current.choices.Single().choiceId, Is.EqualTo("q-wolf"));
        }

        [Test]
        public void Choose_WhenClosed_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => menu.Choose("ask"));
            Assert.That(stateManager.Current.choices, Is.Empty);
        }

        [Test]
        public void Choose_TwiceForOneOpen_SecondCallThrows()
        {
            menu.Open("q-wolf", Options("ask", "leave"));
            menu.Choose("ask");

            Assert.Throws<InvalidOperationException>(() => menu.Choose("leave"));
            Assert.That(stateManager.Current.choices.Single().optionId, Is.EqualTo("ask"));
        }

        [Test]
        public void Choose_UnknownOption_ThrowsAndChangesNothing()
        {
            menu.Open("q-wolf", Options("ask"));

            Assert.Throws<ArgumentException>(() => menu.Choose("fly"));
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(stateManager.Current.choices, Is.Empty);
            Assert.That(stateManager.IsDirty, Is.False);
        }

        private static ChoiceOption[] Options(params string[] ids)
        {
            return ids.Select(id => new ChoiceOption(id, id + ".text")).ToArray();
        }
    }
}
