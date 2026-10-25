using System;
using SnowyWinter.SaveSystem;

namespace SnowyWinter.Dialogue
{
    /// <summary>
    /// Backend of a 1d6 dice check in dialogue. The story calls Open, the UI
    /// shows the roll button on Opened, calls Roll when it is pressed and shows
    /// the result on Rolled. One attempt per check: the result is written to
    /// the playthrough state and a check that has a result is never rolled again.
    /// </summary>
    public sealed class DiceCheck
    {
        public const int Sides = 6;
        public const int SuccessThreshold = 4;
        public const string SuccessOutcomeId = "success";
        public const string FailOutcomeId = "fail";

        private readonly GameStateManager stateManager;
        private readonly IDiceSource dice;

        public DiceCheck(GameStateManager stateManager, IDiceSource dice)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.dice = dice ?? throw new ArgumentNullException(nameof(dice));
        }

        public bool IsOpen { get; private set; }

        public string CheckId { get; private set; } = string.Empty;

        public event Action Opened;

        public event Action<CheckResultData> Rolled;

        public void Open(string checkId)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException($"Check '{CheckId}' is still open.");
            }

            if (string.IsNullOrEmpty(checkId))
            {
                throw new ArgumentException("Check id must not be empty.", nameof(checkId));
            }

            CheckResultData stored = stateManager.Current.checkResults.Find(result => result != null && result.checkId == checkId);
            if (stored != null)
            {
                // Already rolled: give the stored result back instead of a new roll.
                Rolled?.Invoke(stored);
                return;
            }

            CheckId = checkId;
            IsOpen = true;
            Opened?.Invoke();
        }

        public void Roll()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("No check is open.");
            }

            GameStateData state = stateManager.Current;
            int value = dice.Roll(state.playthroughSeed, CheckId);
            if (value < 1 || value > Sides)
            {
                throw new InvalidOperationException($"Dice gave {value}, expected 1..{Sides}.");
            }

            CheckResultData result = new CheckResultData
            {
                checkId = CheckId,
                value = value,
                outcomeId = value >= SuccessThreshold ? SuccessOutcomeId : FailOutcomeId
            };
            state.checkResults.Add(result);

            // Close before notifying, so a handler can open the next check right away.
            CheckId = string.Empty;
            IsOpen = false;

            stateManager.MarkDirty();
            Rolled?.Invoke(result);
        }
    }
}
