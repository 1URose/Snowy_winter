namespace SnowyWinter.Dialogue
{
    /// <summary>
    /// One answer button. Id is the optionId stored in saves; TextKey points
    /// to the text in dialogue content, so the UI never hardcodes answers.
    /// </summary>
    public sealed class ChoiceOption
    {
        public ChoiceOption(string id, string textKey)
        {
            Id = id;
            TextKey = textKey;
        }

        public string Id { get; }
        public string TextKey { get; }
    }
}
