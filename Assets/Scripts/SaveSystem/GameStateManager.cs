using System;

namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Owns the active playthrough state. Gameplay systems can replace the
    /// state or mark it dirty after changing its data.
    /// </summary>
    public sealed class GameStateManager
    {
        public GameStateData Current { get; private set; }

        public bool IsDirty { get; private set; }

        public event Action<GameStateData> StateChanged;

        public GameStateManager(GameStateData initialState = null)
        {
            Current = initialState ?? new GameStateData();
        }

        public void StartNew(GameStateData initialState = null)
        {
            Current = initialState ?? new GameStateData();
            // Guid, not new Random(): two games started in the same tick must not share a seed.
            Current.playthroughSeed = Guid.NewGuid().GetHashCode();
            MarkDirty();
        }

        public void Replace(GameStateData state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Current = state;
            IsDirty = false;
            StateChanged?.Invoke(Current);
        }

        public void MarkDirty()
        {
            IsDirty = true;
            StateChanged?.Invoke(Current);
        }

        public void MarkClean()
        {
            IsDirty = false;
        }
    }
}
