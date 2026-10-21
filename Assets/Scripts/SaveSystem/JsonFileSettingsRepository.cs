using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Stores settings independently from progression slots.
    /// </summary>
    public sealed class JsonFileSettingsRepository : ISettingsRepository
    {
        private const string FileName = "settings.json";
        private readonly string directoryPath;

        public JsonFileSettingsRepository(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A settings directory is required.", nameof(directoryPath));
            }

            this.directoryPath = directoryPath;
        }

        public void Save(SettingsData settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, FileName);
            string temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(settings, true), Encoding.UTF8);
                ReplaceFile(temporaryPath, filePath);
            }
            catch (Exception exception)
            {
                TryDeleteTemporaryFile(temporaryPath);
                throw new SettingsRepositoryException("Could not save settings.", exception);
            }
        }

        public bool TryLoad(out SettingsData settings)
        {
            string filePath = Path.Combine(directoryPath, FileName);
            settings = null;

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                settings = JsonUtility.FromJson<SettingsData>(File.ReadAllText(filePath, Encoding.UTF8));
                if (settings == null || settings.settingsFormatVersion != SaveDataVersions.CurrentSettingsFormatVersion)
                {
                    throw new SettingsRepositoryException("Settings data uses an unsupported format version.");
                }

                return true;
            }
            catch (SettingsRepositoryException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                throw new SettingsRepositoryException("Could not read settings.", exception);
            }
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
                // Preserve the original settings error.
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

    public sealed class SettingsRepositoryException : Exception
    {
        public SettingsRepositoryException(string message)
            : base(message)
        {
        }

        public SettingsRepositoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
