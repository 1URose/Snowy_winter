using System.Collections.Generic;

namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Applies the small, format-level invariants agreed for the DTOs.
    /// Content validity remains the responsibility of the host game.
    /// </summary>
    public static class GameStateNormalizer
    {
        public static void Normalize(GameStateData state)
        {
            if (state == null)
            {
                return;
            }

            state.inventoryItemIds ??= new List<string>();
            state.evidenceIds ??= new List<string>();
            state.completedEventIds ??= new List<string>();
            state.choices ??= new List<ChoiceData>();
            state.flags ??= new List<FlagData>();
            state.checkResults ??= new List<CheckResultData>();

            RemoveDuplicateStrings(state.inventoryItemIds);
            RemoveDuplicateStrings(state.evidenceIds);
            RemoveDuplicateStrings(state.completedEventIds);
            KeepLastChoice(state.choices);
            KeepLastFlag(state.flags);
        }

        private static void RemoveDuplicateStrings(List<string> values)
        {
            HashSet<string> seen = new HashSet<string>();
            for (int index = values.Count - 1; index >= 0; index--)
            {
                if (!seen.Add(values[index]))
                {
                    values.RemoveAt(index);
                }
            }
        }

        private static void KeepLastChoice(List<ChoiceData> choices)
        {
            HashSet<string> seen = new HashSet<string>();
            for (int index = choices.Count - 1; index >= 0; index--)
            {
                ChoiceData choice = choices[index];
                if (choice == null || !seen.Add(choice.choiceId))
                {
                    choices.RemoveAt(index);
                }
            }
        }

        private static void KeepLastFlag(List<FlagData> flags)
        {
            HashSet<string> seen = new HashSet<string>();
            for (int index = flags.Count - 1; index >= 0; index--)
            {
                FlagData flag = flags[index];
                if (flag == null || !seen.Add(flag.flagId))
                {
                    flags.RemoveAt(index);
                }
            }
        }
    }
}
