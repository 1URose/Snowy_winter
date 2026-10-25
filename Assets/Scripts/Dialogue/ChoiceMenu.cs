using System;
using System.Collections.Generic;
using SnowyWinter.SaveSystem;

namespace SnowyWinter.Dialogue
{
    /// <summary>
    /// Backend of the answer choice menu. The UI draws one button per entry of
    /// Options after Opened, calls Choose with the pressed option id and hides
    /// itself on OptionChosen. The pick is written to the playthrough state.
    /// </summary>
    public sealed class ChoiceMenu
    {
        private readonly GameStateManager stateManager;
        private readonly List<ChoiceOption> options = new List<ChoiceOption>();

        public ChoiceMenu(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public bool IsOpen { get; private set; }

        public string ChoiceId { get; private set; } = string.Empty;

        public IReadOnlyList<ChoiceOption> Options => options;

        public event Action Opened;

        public event Action<ChoiceOption> OptionChosen;

        public void Open(string choiceId, IReadOnlyList<ChoiceOption> newOptions)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException($"Choice '{ChoiceId}' is still open.");
            }

            if (string.IsNullOrEmpty(choiceId))
            {
                throw new ArgumentException("Choice id must not be empty.", nameof(choiceId));
            }

            if (newOptions == null || newOptions.Count == 0)
            {
                throw new ArgumentException("A choice needs at least one option.", nameof(newOptions));
            }

            HashSet<string> ids = new HashSet<string>();
            foreach (ChoiceOption option in newOptions)
            {
                if (option == null || string.IsNullOrEmpty(option.Id))
                {
                    throw new ArgumentException("Every option needs a non-empty id.", nameof(newOptions));
                }

                if (!ids.Add(option.Id))
                {
                    throw new ArgumentException($"Duplicate option id '{option.Id}'.", nameof(newOptions));
                }
            }

            // Copy, so the caller cannot change the buttons while the menu is open.
            options.AddRange(newOptions);
            ChoiceId = choiceId;
            IsOpen = true;
            Opened?.Invoke();
        }

        public void Choose(string optionId)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("No choice is open.");
            }

            ChoiceOption chosen = options.Find(option => option.Id == optionId)
                ?? throw new ArgumentException($"Choice '{ChoiceId}' has no option '{optionId}'.", nameof(optionId));

            GameStateData state = stateManager.Current;
            string choiceId = ChoiceId;
            // One entry per choiceId: a repeated choice replaces the old answer.
            state.choices.RemoveAll(choice => choice.choiceId == choiceId);
            state.choices.Add(new ChoiceData
            {
                choiceId = choiceId,
                optionId = chosen.Id,
                selectedAtCheckpointId = state.checkpointId
            });

            // Close before notifying, so a handler can open the next choice right away.
            options.Clear();
            ChoiceId = string.Empty;
            IsOpen = false;

            stateManager.MarkDirty();
            OptionChosen?.Invoke(chosen);
        }
    }
}
