namespace SnowyWinter.SaveSystem
{
    /// <summary>
    /// Storage boundary for progression saves. The game does not need to know
    /// whether the implementation uses JSON files, cloud storage, or another
    /// persistence mechanism.
    /// </summary>
    public interface ISaveRepository
    {
        void Save(string slotId, SaveFileData data);

        bool TryLoad(string slotId, out SaveFileData data);

        bool TryGetMetadata(string slotId, out SaveSlotMetadata metadata);

        bool Exists(string slotId);

        void Delete(string slotId);
    }
}
