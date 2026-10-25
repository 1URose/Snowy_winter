using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using SnowyWinter.SaveSystem;

namespace SnowyWinter.Tests
{
    public sealed class SaveSystemTests
    {
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "SnowyWinterSaveTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void JsonRepository_RoundTripsAllSaveFields()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            SaveFileData original = new SaveFileData
            {
                gameVersion = "1.2.3",
                metadata = new SaveSlotMetadata
                {
                    slotId = "slot-1", saveType = "manual",
                    createdAtUtc = "2026-01-01T10:00:00.0000000Z",
                    updatedAtUtc = "2026-01-01T10:01:00.0000000Z",
                    chapterId = "chapter-2", locationId = "tavern",
                    displayName = "Tavern", summary = "Before the captain", playTimeSeconds = 321.5f
                },
                gameState = new GameStateData
                {
                    chapterId = "chapter-2", locationId = "tavern", checkpointId = "checkpoint-3",
                    dialogueNodeId = "captain-intro", playTimeSeconds = 321.5f, playthroughSeed = -123456789,
                    inventoryItemIds = { "letter", "key" }, evidenceIds = { "rope-mark" },
                    completedEventIds = { "met-sailor" },
                    choices = { new ChoiceData { choiceId = "route", optionId = "captain", selectedAtCheckpointId = "checkpoint-2" } },
                    flags = { new FlagData { flagId = "captainFound", value = true } },
                    checkResults = { new CheckResultData { checkId = "guard", value = 5, outcomeId = "success" } }
                }
            };

            repository.Save("slot-1", original);

            Assert.That(repository.TryLoad("slot-1", out SaveFileData loaded), Is.True);
            Assert.That(loaded.gameVersion, Is.EqualTo("1.2.3"));
            Assert.That(loaded.metadata.saveType, Is.EqualTo("manual"));
            Assert.That(loaded.metadata.createdAtUtc, Is.EqualTo(original.metadata.createdAtUtc));
            Assert.That(loaded.metadata.updatedAtUtc, Is.EqualTo(original.metadata.updatedAtUtc));
            Assert.That(loaded.metadata.chapterId, Is.EqualTo("chapter-2"));
            Assert.That(loaded.metadata.locationId, Is.EqualTo("tavern"));
            Assert.That(loaded.metadata.displayName, Is.EqualTo("Tavern"));
            Assert.That(loaded.metadata.summary, Is.EqualTo("Before the captain"));
            Assert.That(loaded.metadata.playTimeSeconds, Is.EqualTo(321.5f));
            Assert.That(loaded.gameState.checkpointId, Is.EqualTo("checkpoint-3"));
            Assert.That(loaded.gameState.dialogueNodeId, Is.EqualTo("captain-intro"));
            Assert.That(loaded.gameState.playthroughSeed, Is.EqualTo(-123456789));
            Assert.That(loaded.gameState.inventoryItemIds, Is.EqualTo(new[] { "letter", "key" }));
            Assert.That(loaded.gameState.evidenceIds, Is.EqualTo(new[] { "rope-mark" }));
            Assert.That(loaded.gameState.completedEventIds, Is.EqualTo(new[] { "met-sailor" }));
            Assert.That(loaded.gameState.choices[0].optionId, Is.EqualTo("captain"));
            Assert.That(loaded.gameState.flags[0].value, Is.True);
            Assert.That(loaded.gameState.checkResults[0].value, Is.EqualTo(5));
        }

        [Test]
        public void JsonRepository_LoadsMissingSlotAsFalse()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);

            Assert.That(repository.TryLoad("missing", out SaveFileData data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void JsonRepository_ExistsAndDeleteReflectSlotLifecycle()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);

            Assert.That(repository.Exists("slot-1"), Is.False);
            repository.Save("slot-1", CreateSave("slot-1"));
            Assert.That(repository.Exists("slot-1"), Is.True);
            repository.Delete("slot-1");
            Assert.That(repository.Exists("slot-1"), Is.False);
            Assert.That(repository.TryLoad("slot-1", out _), Is.False);
        }

        [Test]
        public void JsonRepository_DeleteMissingSlotDoesNotThrow()
        {
            Assert.DoesNotThrow(() => new JsonFileSaveRepository(temporaryDirectory).Delete("missing"));
        }

        [Test]
        public void JsonRepository_ReturnsMetadataForExistingSlotAndFalseForMissingSlot()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            SaveFileData data = CreateSave("slot-1");
            data.metadata.displayName = "Port";
            repository.Save("slot-1", data);

            Assert.That(repository.TryGetMetadata("slot-1", out SaveSlotMetadata metadata), Is.True);
            Assert.That(metadata.displayName, Is.EqualTo("Port"));
            Assert.That(repository.TryGetMetadata("missing", out SaveSlotMetadata missingMetadata), Is.False);
            Assert.That(missingMetadata, Is.Null);
        }

        [Test]
        public void JsonRepository_FillsEmptyMetadataSlotIdWithRequestedSlotId()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            SaveFileData data = CreateSave(string.Empty);

            repository.Save("slot-1", data);

            Assert.That(data.metadata.slotId, Is.EqualTo("slot-1"));
            Assert.That(repository.TryLoad("slot-1", out SaveFileData loaded), Is.True);
            Assert.That(loaded.metadata.slotId, Is.EqualTo("slot-1"));
        }

        [Test]
        public void JsonRepository_RejectsMismatchedMetadataSlotId()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            Assert.Throws<SaveRepositoryException>(() => repository.Save("slot-1", CreateSave("other-slot")));
        }

        [Test]
        public void JsonRepository_RejectsIncompleteSaveData()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);

            Assert.Throws<SaveRepositoryException>(() => repository.Save("slot-1", null));
            Assert.Throws<SaveRepositoryException>(() => repository.Save("slot-1", new SaveFileData { metadata = null }));
            Assert.Throws<SaveRepositoryException>(() => repository.Save("slot-1", new SaveFileData { gameState = null }));
        }

        [Test]
        public void JsonRepository_RejectsUnsupportedVersionWhenSavingAndLoading()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            SaveFileData unsupported = CreateSave("slot-1");
            unsupported.saveFormatVersion = SaveDataVersions.CurrentSaveFormatVersion + 1;
            Assert.Throws<SaveRepositoryException>(() => repository.Save("slot-1", unsupported));

            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(Path.Combine(temporaryDirectory, "slot-1.json"), "{\"saveFormatVersion\":99,\"metadata\":{\"slotId\":\"slot-1\"},\"gameState\":{}}");
            Assert.Throws<SaveRepositoryException>(() => repository.TryLoad("slot-1", out _));
        }

        [Test]
        public void JsonRepository_ThrowsRepositoryExceptionForCorruptedJson()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(Path.Combine(temporaryDirectory, "slot-1.json"), "{ invalid-json");

            Assert.Throws<SaveRepositoryException>(() => repository.TryLoad("slot-1", out _));
        }

        [Test]
        public void JsonRepository_NormalizesNullCollectionsAfterLoad()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(Path.Combine(temporaryDirectory, "slot-1.json"), "{\"saveFormatVersion\":1,\"metadata\":{\"slotId\":\"slot-1\"},\"gameState\":{\"inventoryItemIds\":null,\"evidenceIds\":null,\"completedEventIds\":null,\"choices\":null,\"flags\":null,\"checkResults\":null}}");

            Assert.That(repository.TryLoad("slot-1", out SaveFileData loaded), Is.True);
            Assert.That(loaded.gameState.inventoryItemIds, Is.Empty);
            Assert.That(loaded.gameState.evidenceIds, Is.Empty);
            Assert.That(loaded.gameState.completedEventIds, Is.Empty);
            Assert.That(loaded.gameState.choices, Is.Empty);
            Assert.That(loaded.gameState.flags, Is.Empty);
            Assert.That(loaded.gameState.checkResults, Is.Empty);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("../outside")]
        [TestCase("slot/name")]
        [TestCase("slot.name")]
        public void JsonRepository_RejectsInvalidSlotIds(string slotId)
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);

            Assert.Throws<ArgumentException>(() => repository.Exists(slotId));
            Assert.Throws<ArgumentException>(() => repository.Save(slotId, CreateSave("slot-1")));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void JsonRepository_RejectsEmptyDirectoryPath(string directoryPath)
        {
            Assert.Throws<ArgumentException>(() => new JsonFileSaveRepository(directoryPath));
        }

        [Test]
        public void Normalizer_InitializesCollectionsAndRemovesDuplicateStringIds()
        {
            GameStateData state = new GameStateData
            {
                inventoryItemIds = null,
                evidenceIds = new List<string> { "mark", "rope", "mark" },
                completedEventIds = new List<string> { "event-a", "event-b", "event-a" },
                choices = null,
                flags = null,
                checkResults = null
            };

            GameStateNormalizer.Normalize(state);

            Assert.That(state.inventoryItemIds, Is.Empty);
            Assert.That(state.evidenceIds, Is.EqualTo(new[] { "rope", "mark" }));
            Assert.That(state.completedEventIds, Is.EqualTo(new[] { "event-b", "event-a" }));
            Assert.That(state.choices, Is.Empty);
            Assert.That(state.flags, Is.Empty);
            Assert.That(state.checkResults, Is.Empty);
        }

        [Test]
        public void Normalizer_KeepsLastChoiceAndFlagAndRemovesNullEntries()
        {
            GameStateData state = new GameStateData
            {
                choices = new List<ChoiceData>
                {
                    new ChoiceData { choiceId = "route", optionId = "captain" }, null,
                    new ChoiceData { choiceId = "money", optionId = "keep" },
                    new ChoiceData { choiceId = "route", optionId = "rebels" }
                },
                flags = new List<FlagData>
                {
                    new FlagData { flagId = "captainFound", value = false }, null,
                    new FlagData { flagId = "letterOpened", value = true },
                    new FlagData { flagId = "captainFound", value = true }
                }
            };

            GameStateNormalizer.Normalize(state);

            Assert.That(state.choices, Has.Count.EqualTo(2));
            Assert.That(state.choices[0].choiceId, Is.EqualTo("money"));
            Assert.That(state.choices[1].optionId, Is.EqualTo("rebels"));
            Assert.That(state.flags, Has.Count.EqualTo(2));
            Assert.That(state.flags[0].flagId, Is.EqualTo("letterOpened"));
            Assert.That(state.flags[1].value, Is.True);
        }

        [Test]
        public void Normalizer_DoesNotAlterCheckResultsAndAllowsNullState()
        {
            GameStateData state = new GameStateData
            {
                checkResults = new List<CheckResultData>
                {
                    new CheckResultData { checkId = "guard", value = 2, outcomeId = "failure" },
                    new CheckResultData { checkId = "guard", value = 5, outcomeId = "success" }
                }
            };

            Assert.DoesNotThrow(() => GameStateNormalizer.Normalize(null));
            GameStateNormalizer.Normalize(state);

            Assert.That(state.checkResults, Has.Count.EqualTo(2));
            Assert.That(state.checkResults[0].value, Is.EqualTo(2));
            Assert.That(state.checkResults[1].value, Is.EqualTo(5));
        }

        [Test]
        public void GameStateManager_ManagesStateDirtyFlagAndEvents()
        {
            GameStateData initial = new GameStateData { chapterId = "initial" };
            GameStateManager manager = new GameStateManager(initial);
            int eventCount = 0;
            GameStateData lastEventState = null;
            manager.StateChanged += state => { eventCount++; lastEventState = state; };

            Assert.That(manager.Current, Is.SameAs(initial));
            Assert.That(manager.IsDirty, Is.False);

            GameStateData newState = new GameStateData { chapterId = "new" };
            manager.StartNew(newState);
            Assert.That(manager.Current, Is.SameAs(newState));
            Assert.That(manager.IsDirty, Is.True);
            Assert.That(lastEventState, Is.SameAs(newState));

            manager.MarkClean();
            Assert.That(manager.IsDirty, Is.False);
            Assert.That(eventCount, Is.EqualTo(1));

            manager.MarkDirty();
            Assert.That(manager.IsDirty, Is.True);
            Assert.That(eventCount, Is.EqualTo(2));

            GameStateData loadedState = new GameStateData { chapterId = "loaded" };
            manager.Replace(loadedState);
            Assert.That(manager.Current, Is.SameAs(loadedState));
            Assert.That(manager.IsDirty, Is.False);
            Assert.That(eventCount, Is.EqualTo(3));
            Assert.Throws<ArgumentNullException>(() => manager.Replace(null));
        }

        [Test]
        public void GameStateManager_CreatesDefaultStateWhenInitialStateIsNull()
        {
            GameStateManager manager = new GameStateManager();
            Assert.That(manager.Current, Is.Not.Null);
            Assert.That(manager.IsDirty, Is.False);
        }

        [Test]
        public void GameStateManager_StartNewGivesEachPlaythroughItsOwnSeed()
        {
            GameStateManager manager = new GameStateManager();

            manager.StartNew();
            int firstSeed = manager.Current.playthroughSeed;
            manager.StartNew(new GameStateData { playthroughSeed = firstSeed });

            Assert.That(manager.Current.playthroughSeed, Is.Not.EqualTo(firstSeed));
        }

        [Test]
        public void SaveService_SaveWritesDerivedMetadataNormalizesStateAndMarksClean()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            GameStateManager manager = new GameStateManager(new GameStateData
            {
                chapterId = "chapter-3", locationId = "deck", playTimeSeconds = 44.5f,
                inventoryItemIds = new List<string> { "letter", "letter" }
            });
            SaveService service = new SaveService(repository, manager, "2.0.0");
            manager.MarkDirty();

            service.Save("slot-1", "auto", "Deck", "Before final choice");

            Assert.That(manager.IsDirty, Is.False);
            Assert.That(repository.TryLoad("slot-1", out SaveFileData saved), Is.True);
            Assert.That(saved.gameVersion, Is.EqualTo("2.0.0"));
            Assert.That(saved.metadata.saveType, Is.EqualTo("auto"));
            Assert.That(saved.metadata.chapterId, Is.EqualTo("chapter-3"));
            Assert.That(saved.metadata.locationId, Is.EqualTo("deck"));
            Assert.That(saved.metadata.playTimeSeconds, Is.EqualTo(44.5f));
            Assert.That(saved.gameState.inventoryItemIds, Is.EqualTo(new[] { "letter" }));
            Assert.That(DateTime.TryParse(saved.metadata.createdAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _), Is.True);
            Assert.That(DateTime.TryParse(saved.metadata.updatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _), Is.True);
        }

        [Test]
        public void SaveService_OverwritingSlotPreservesCreatedAndNullTextButUpdatesDynamicMetadata()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            GameStateManager manager = new GameStateManager(new GameStateData { chapterId = "chapter-1", locationId = "street", playTimeSeconds = 10f });
            SaveService service = new SaveService(repository, manager, "test");

            service.Save("slot-1", "manual", "Port", "First state");
            repository.TryGetMetadata("slot-1", out SaveSlotMetadata firstMetadata);

            manager.Current.chapterId = "chapter-2";
            manager.Current.locationId = "tavern";
            manager.Current.playTimeSeconds = 20f;
            manager.MarkDirty();
            service.Save("slot-1", "auto");
            repository.TryGetMetadata("slot-1", out SaveSlotMetadata secondMetadata);

            Assert.That(secondMetadata.createdAtUtc, Is.EqualTo(firstMetadata.createdAtUtc));
            Assert.That(secondMetadata.displayName, Is.EqualTo("Port"));
            Assert.That(secondMetadata.summary, Is.EqualTo("First state"));
            Assert.That(secondMetadata.saveType, Is.EqualTo("auto"));
            Assert.That(secondMetadata.chapterId, Is.EqualTo("chapter-2"));
            Assert.That(secondMetadata.locationId, Is.EqualTo("tavern"));
            Assert.That(secondMetadata.playTimeSeconds, Is.EqualTo(20f));
        }

        [Test]
        public void SaveService_AllowsEmptyTextToExplicitlyClearMetadata()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            SaveService service = new SaveService(repository, new GameStateManager());

            service.Save("slot-1", "manual", "Port", "Description");
            service.Save("slot-1", "manual", string.Empty, string.Empty);

            Assert.That(repository.TryGetMetadata("slot-1", out SaveSlotMetadata metadata), Is.True);
            Assert.That(metadata.displayName, Is.Empty);
            Assert.That(metadata.summary, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SaveService_RejectsInvalidSaveType(string saveType)
        {
            SaveService service = new SaveService(new JsonFileSaveRepository(temporaryDirectory), new GameStateManager());
            Assert.Throws<ArgumentException>(() => service.Save("slot-1", saveType));
        }

        [Test]
        public void SaveService_LoadsExistingStateAndLeavesCurrentStateOnMissingSlot()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            GameStateManager manager = new GameStateManager(new GameStateData { chapterId = "active" });
            SaveService service = new SaveService(repository, manager);
            GameStateData activeState = manager.Current;
            manager.MarkDirty();

            Assert.That(service.Load("missing"), Is.False);
            Assert.That(manager.Current, Is.SameAs(activeState));
            Assert.That(manager.IsDirty, Is.True);

            SaveFileData fileData = CreateSave("slot-1");
            fileData.gameState.choices = new List<ChoiceData>
            {
                new ChoiceData { choiceId = "route", optionId = "captain" },
                new ChoiceData { choiceId = "route", optionId = "rebels" }
            };
            repository.Save("slot-1", fileData);

            Assert.That(service.Load("slot-1"), Is.True);
            Assert.That(manager.Current.choices, Has.Count.EqualTo(1));
            Assert.That(manager.Current.choices[0].optionId, Is.EqualTo("rebels"));
            Assert.That(manager.IsDirty, Is.False);
        }

        [Test]
        public void SaveService_DelegatesSlotOperationsAndRejectsNullDependencies()
        {
            JsonFileSaveRepository repository = new JsonFileSaveRepository(temporaryDirectory);
            GameStateManager manager = new GameStateManager();
            SaveService service = new SaveService(repository, manager);

            service.Save("slot-1", "manual");
            Assert.That(service.Exists("slot-1"), Is.True);
            Assert.That(service.TryGetMetadata("slot-1", out SaveSlotMetadata metadata), Is.True);
            Assert.That(metadata.slotId, Is.EqualTo("slot-1"));
            service.Delete("slot-1");
            Assert.That(service.Exists("slot-1"), Is.False);
            Assert.Throws<ArgumentNullException>(() => new SaveService(null, manager));
            Assert.Throws<ArgumentNullException>(() => new SaveService(repository, null));
        }

        [Test]
        public void SettingsRepository_RoundTripsAllSettingsFields()
        {
            JsonFileSettingsRepository repository = new JsonFileSettingsRepository(temporaryDirectory);
            SettingsData original = new SettingsData
            {
                masterVolume = 0.1f, musicVolume = 0.2f, effectsVolume = 0.3f, voiceVolume = 0.4f,
                textSpeed = 1.5f, autoAdvanceText = true, languageId = "ru", fullscreen = false,
                resolutionWidth = 1920, resolutionHeight = 1080
            };

            repository.Save(original);

            Assert.That(repository.TryLoad(out SettingsData loaded), Is.True);
            Assert.That(loaded.masterVolume, Is.EqualTo(0.1f));
            Assert.That(loaded.musicVolume, Is.EqualTo(0.2f));
            Assert.That(loaded.effectsVolume, Is.EqualTo(0.3f));
            Assert.That(loaded.voiceVolume, Is.EqualTo(0.4f));
            Assert.That(loaded.textSpeed, Is.EqualTo(1.5f));
            Assert.That(loaded.autoAdvanceText, Is.True);
            Assert.That(loaded.languageId, Is.EqualTo("ru"));
            Assert.That(loaded.fullscreen, Is.False);
            Assert.That(loaded.resolutionWidth, Is.EqualTo(1920));
            Assert.That(loaded.resolutionHeight, Is.EqualTo(1080));
        }

        [Test]
        public void SettingsRepository_HandlesMissingInvalidAndCorruptedFiles()
        {
            JsonFileSettingsRepository repository = new JsonFileSettingsRepository(temporaryDirectory);

            Assert.That(repository.TryLoad(out SettingsData missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.Throws<ArgumentNullException>(() => repository.Save(null));

            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(Path.Combine(temporaryDirectory, "settings.json"), "{\"settingsFormatVersion\":99}");
            Assert.Throws<SettingsRepositoryException>(() => repository.TryLoad(out _));

            File.WriteAllText(Path.Combine(temporaryDirectory, "settings.json"), "{ invalid-json");
            Assert.Throws<SettingsRepositoryException>(() => repository.TryLoad(out _));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SettingsRepository_RejectsEmptyDirectoryPath(string directoryPath)
        {
            Assert.Throws<ArgumentException>(() => new JsonFileSettingsRepository(directoryPath));
        }

        [Test]
        public void SettingsService_UsesDefaultsWhenNoFileExistsAndPersistsCurrentSettings()
        {
            MemorySettingsRepository repository = new MemorySettingsRepository();
            SettingsService service = new SettingsService(repository);
            int eventCount = 0;
            service.SettingsChanged += _ => eventCount++;

            service.Load();
            Assert.That(service.Current, Is.Not.Null);
            Assert.That(service.Current.masterVolume, Is.EqualTo(1f));
            Assert.That(eventCount, Is.EqualTo(0));

            service.Current.masterVolume = 0.5f;
            service.Save();
            Assert.That(repository.Stored.masterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public void SettingsService_LoadReplaceAndMarkChangedRaiseEvents()
        {
            MemorySettingsRepository repository = new MemorySettingsRepository { Stored = new SettingsData { languageId = "en" } };
            SettingsService service = new SettingsService(repository);
            int eventCount = 0;
            SettingsData lastSettings = null;
            service.SettingsChanged += settings => { eventCount++; lastSettings = settings; };

            service.Load();
            Assert.That(service.Current.languageId, Is.EqualTo("en"));
            Assert.That(eventCount, Is.EqualTo(1));

            SettingsData replacement = new SettingsData { languageId = "ru" };
            service.Replace(replacement);
            Assert.That(lastSettings, Is.SameAs(replacement));
            Assert.That(eventCount, Is.EqualTo(2));

            service.Current.textSpeed = 2f;
            service.MarkChanged();
            Assert.That(lastSettings.textSpeed, Is.EqualTo(2f));
            Assert.That(eventCount, Is.EqualTo(3));
            Assert.Throws<ArgumentNullException>(() => service.Replace(null));
            Assert.Throws<ArgumentNullException>(() => new SettingsService(null));
        }

        private static SaveFileData CreateSave(string slotId)
        {
            return new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = slotId, saveType = "manual" },
                gameState = new GameStateData()
            };
        }

        private sealed class MemorySettingsRepository : ISettingsRepository
        {
            public SettingsData Stored;

            public void Save(SettingsData settings)
            {
                Stored = settings;
            }

            public bool TryLoad(out SettingsData settings)
            {
                settings = Stored;
                return settings != null;
            }
        }
    }
}
