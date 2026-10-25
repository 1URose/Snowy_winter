using System;
using System.Collections.Generic;

namespace SnowyWinter.SaveSystem
{
    public static class SaveDataVersions
    {
        public const int CurrentSaveFormatVersion = 1;
        public const int CurrentSettingsFormatVersion = 1;
    }

    /// <summary>
    /// Serializable state of a playthrough. It stores stable identifiers and
    /// values, not references to scene objects or Unity assets.
    /// </summary>
    [Serializable]
    public sealed class GameStateData
    {
        public string chapterId = string.Empty;
        public string locationId = string.Empty;
        public string checkpointId = string.Empty;
        public string dialogueNodeId = string.Empty;

        public float playTimeSeconds;

        // Set once per playthrough. Dice checks derive their values from it,
        // so reloading a save cannot reroll a check.
        public int playthroughSeed;

        public List<string> inventoryItemIds = new List<string>();
        public List<string> evidenceIds = new List<string>();
        public List<string> completedEventIds = new List<string>();
        // One entry per choiceId. A later selection replaces the previous one.
        public List<ChoiceData> choices = new List<ChoiceData>();
        // One entry per flagId. The value is the current value of that flag.
        public List<FlagData> flags = new List<FlagData>();
        public List<CheckResultData> checkResults = new List<CheckResultData>();
    }

    /// <summary>
    /// Settings are intentionally separate from GameStateData and therefore
    /// are not tied to a particular playthrough or save slot.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public int settingsFormatVersion = SaveDataVersions.CurrentSettingsFormatVersion;

        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float effectsVolume = 1f;
        public float voiceVolume = 1f;

        public float textSpeed = 1f;
        public bool autoAdvanceText;
        public string languageId = string.Empty;

        public bool fullscreen = true;
        public int resolutionWidth;
        public int resolutionHeight;
    }

    /// <summary>
    /// Metadata snapshot used to render a save slot. It is stored together
    /// with the complete state and is not the source of truth on load.
    /// </summary>
    [Serializable]
    public sealed class SaveSlotMetadata
    {
        public string slotId = string.Empty;
        public string saveType = string.Empty;

        public string createdAtUtc = string.Empty;
        public string updatedAtUtc = string.Empty;

        public string chapterId = string.Empty;
        public string locationId = string.Empty;
        public string displayName = string.Empty;
        public string summary = string.Empty;
        public float playTimeSeconds;
    }

    /// <summary>
    /// The contents of one progression save file. Settings are stored in a
    /// separate file and are deliberately not included here.
    /// </summary>
    [Serializable]
    public sealed class SaveFileData
    {
        public int saveFormatVersion = SaveDataVersions.CurrentSaveFormatVersion;
        public string gameVersion = string.Empty;
        public SaveSlotMetadata metadata = new SaveSlotMetadata();
        public GameStateData gameState = new GameStateData();
    }

    [Serializable]
    public sealed class ChoiceData
    {
        public string choiceId = string.Empty;
        public string optionId = string.Empty;
        public string selectedAtCheckpointId = string.Empty;
    }

    [Serializable]
    public sealed class FlagData
    {
        public string flagId = string.Empty;
        public bool value;
    }

    [Serializable]
    public sealed class CheckResultData
    {
        public string checkId = string.Empty;
        public int value;
        public string outcomeId = string.Empty;
    }
}
