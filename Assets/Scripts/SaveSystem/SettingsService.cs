using System;

namespace SnowyWinter.SaveSystem
{
    public interface ISettingsRepository
    {
        void Save(SettingsData settings);

        bool TryLoad(out SettingsData settings);
    }

    /// <summary>
    /// Owns settings independently from progression saves.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly ISettingsRepository repository;

        public SettingsData Current { get; private set; }

        public event Action<SettingsData> SettingsChanged;

        public SettingsService(ISettingsRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Current = new SettingsData();
        }

        public void Load()
        {
            if (repository.TryLoad(out SettingsData settings) && settings != null)
            {
                Current = settings;
                SettingsChanged?.Invoke(Current);
            }
        }

        public void Save()
        {
            repository.Save(Current);
        }

        /// <summary>
        /// Call after mutating fields on Current so subscribers can apply the
        /// new settings. Keeping this explicit avoids a reflective change
        /// tracking system in the generic tool.
        /// </summary>
        public void MarkChanged()
        {
            SettingsChanged?.Invoke(Current);
        }

        public void Replace(SettingsData settings)
        {
            Current = settings ?? throw new ArgumentNullException(nameof(settings));
            SettingsChanged?.Invoke(Current);
        }
    }
}
