namespace HealthNote.Api.Contracts
{
    public sealed class SaveEncounterRequest
    {
        public int? ExpectedVersion { get; init; }
        public string? VisitDate { get; init; }
        public string? ChiefComplaint { get; init; }
        public string? Assessment { get; init; }
        public string? Plan { get; init; }
    }
}
