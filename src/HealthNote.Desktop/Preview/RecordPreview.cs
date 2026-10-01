namespace HealthNote.Desktop.Preview
{
    // Presentation fixture only; not a healthcare entity or API contract.
    public sealed class RecordPreview
    {
        public RecordPreview(string id, string type, string title, string date, string summary)
        {
            Id = id;
            Type = type;
            Title = title;
            Date = date;
            Summary = summary;
        }

        public string Id { get; }
        public string Type { get; }
        public string Title { get; }
        public string Date { get; }
        public string Summary { get; }
        public string Source => Type + " 기록 · 합성 데모";
        public string AccessibleName => Date + " " + Type + " " + Title;
    }
}
