using System;

namespace HealthNote.Domain.Emr
{
    public sealed class EncounterNote
    {
        public EncounterNote(Guid id, Guid patientId, EncounterContent content, int version, DateTime savedAtUtc)
        {
            if (id == Guid.Empty || patientId == Guid.Empty) throw new ArgumentException("IDs are required.");
            if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
            if (savedAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC is required.", nameof(savedAtUtc));
            Id = id;
            PatientId = patientId;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Version = version;
            SavedAtUtc = savedAtUtc;
        }

        public Guid Id { get; }
        public Guid PatientId { get; }
        public EncounterContent Content { get; }
        public int Version { get; }
        public DateTime SavedAtUtc { get; }
    }
}
