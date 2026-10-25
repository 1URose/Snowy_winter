namespace SnowyWinter.Dialogue
{
    /// <summary>
    /// Gives the die value (1..6) for a check. The same seed and check id must
    /// always give the same value. Tests substitute their own values here.
    /// </summary>
    public interface IDiceSource
    {
        int Roll(int playthroughSeed, string checkId);
    }
}
