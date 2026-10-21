using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Small JSON-file implementation of ISaveRepository. The root directory
    /// is injected so the repository can be used with any storage location.
    /// </summary>
    public sealed class JsonFileSaveRepository : ISaveRepository
    {
        private const string FileExtension = ".json";
        private readonly string directoryPath;

        public JsonFileSaveRepository(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A save directory is required.", nameof(directoryPath));
            }

            this.directoryPath = directoryPath;
        }

        public void Save(string slotId, SaveFileData data)
        {
            string filePath = GetFilePath(slotId);
            ValidateData(slotId, data);

            Directory.CreateDirectory(directoryPath);

            string temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string json = JsonUtility.ToJson(data, true);

            try
            {
                File.WriteAllText(temporaryPath, json, Encoding.UTF8);
                ReplaceFile(temporaryPath, filePath);
            }
            catch (Exception exception)
            {
                TryDeleteTemporaryFile(temporaryPath);
                throw new SaveRepositoryException("Could not save slot '" + slotId + "'.", exception);
            }
        }

        public bool TryLoad(string slotId, out SaveFileData data)
        {
            string filePath = GetFilePath(slotId);
            data = null;

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                data = JsonUtility.FromJson<SaveFileData>(json);
                ValidateData(slotId, data);
                NormalizeLists(data);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                throw new SaveRepositoryException("Could not read slot '" + slotId + "'.", exception);
            }
        }

        public bool Exists(string slotId)
        {
            return File.Exists(GetFilePath(slotId));
        }

        public bool TryGetMetadata(string slotId, out SaveSlotMetadata metadata)
        {
            metadata = null;

            if (!TryLoad(slotId, out SaveFileData data))
            {
                return false;
            }

            metadata = data.metadata;
            return true;
        }

        public void Delete(string slotId)
        {
            string filePath = GetFilePath(slotId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string GetFilePath(string slotId)
        {
            ValidateSlotId(slotId);
            return Path.Combine(directoryPath, slotId + FileExtension);
        }

        private static void ValidateSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("A save slot ID is required.", nameof(slotId));
            }

            for (int index = 0; index < slotId.Length; index++)
            {
                char character = slotId[index];
                bool isAllowed = char.IsLetterOrDigit(character) || character == '_' || character == '-';

                if (!isAllowed)
                {
                    throw new ArgumentException("Save slot IDs may contain only letters, numbers, '_' and '-'.", nameof(slotId));
                }
            }
        }

        private static void ValidateData(string slotId, SaveFileData data)
        {
            if (data == null || data.metadata == null || data.gameState == null)
            {
                throw new SaveRepositoryException("Save data is incomplete for slot '" + slotId + "'.");
            }

            if (data.saveFormatVersion != SaveDataVersions.CurrentSaveFormatVersion)
            {
                throw new SaveRepositoryException("Save data uses an unsupported format version.");
            }

            if (string.IsNullOrEmpty(data.metadata.slotId))
            {
                data.metadata.slotId = slotId;
            }
            else if (data.metadata.slotId != slotId)
            {
                throw new SaveRepositoryException("Save metadata does not match the requested slot.");
            }
        }

        private static void NormalizeLists(SaveFileData data)
        {
            GameStateData state = data.gameState;
            state.inventoryItemIds ??= new System.Collections.Generic.List<string>();
            state.evidenceIds ??= new System.Collections.Generic.List<string>();
            state.completedEventIds ??= new System.Collections.Generic.List<string>();
            state.choices ??= new System.Collections.Generic.List<ChoiceData>();
            state.flags ??= new System.Collections.Generic.List<FlagData>();
            state.checkResults ??= new System.Collections.Generic.List<CheckResultData>();
        }

        private static void TryDeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
                // Preserve the original save error.
            }
        }

        private static void ReplaceFile(string temporaryPath, string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Replace(temporaryPath, filePath, null);
            }
            else
            {
                File.Move(temporaryPath, filePath);
            }
        }
    }

    public sealed class SaveRepositoryException : Exception
    {
        public SaveRepositoryException(string message)
            : base(message)
        {
        }

        public SaveRepositoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
