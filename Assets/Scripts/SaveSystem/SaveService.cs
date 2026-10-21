using System;
using System.Globalization;

namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Coordinates the active game state and a persistence repository. It does
    /// not decide when saving is allowed or how a loaded scene is restored.
    /// </summary>
    public sealed class SaveService
    {
        private readonly ISaveRepository repository;
        private readonly GameStateManager stateManager;
        private readonly string gameVersion;

        public SaveService(ISaveRepository repository, GameStateManager stateManager, string gameVersion = "")
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.gameVersion = gameVersion ?? string.Empty;
        }

        public void Save(string slotId, string saveType, string displayName = null, string summary = null)
        {
            if (string.IsNullOrWhiteSpace(saveType))
            {
                throw new ArgumentException("A save type is required.", nameof(saveType));
            }

            GameStateNormalizer.Normalize(stateManager.Current);

            string createdAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            SaveSlotMetadata previousMetadata = null;
            if (repository.TryLoad(slotId, out SaveFileData previousData) && previousData.metadata != null)
            {
                previousMetadata = previousData.metadata;
                createdAtUtc = previousMetadata.createdAtUtc;
            }

            SaveFileData data = new SaveFileData
            {
                gameVersion = gameVersion,
                metadata = new SaveSlotMetadata
                {
                    slotId = slotId,
                    saveType = saveType,
                    createdAtUtc = createdAtUtc,
                    updatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    chapterId = stateManager.Current.chapterId,
                    locationId = stateManager.Current.locationId,
                    displayName = displayName ?? previousMetadata?.displayName ?? string.Empty,
                    summary = summary ?? previousMetadata?.summary ?? string.Empty,
                    playTimeSeconds = stateManager.Current.playTimeSeconds
                },
                gameState = stateManager.Current
            };

            repository.Save(slotId, data);
            stateManager.MarkClean();
        }

        public bool Load(string slotId)
        {
            if (!repository.TryLoad(slotId, out SaveFileData data))
            {
                return false;
            }

            GameStateNormalizer.Normalize(data.gameState);
            stateManager.Replace(data.gameState);
            return true;
        }

        public bool Exists(string slotId)
        {
            return repository.Exists(slotId);
        }

        public bool TryGetMetadata(string slotId, out SaveSlotMetadata metadata)
        {
            return repository.TryGetMetadata(slotId, out metadata);
        }

        public void Delete(string slotId)
        {
            repository.Delete(slotId);
        }
    }
}
