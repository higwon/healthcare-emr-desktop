using HealthNote.Domain.Emr;

namespace HealthNote.Application.Emr
{
    public enum SaveEncounterStatus { Created, Updated, NotFound, Conflict }

    public sealed class SaveEncounterResult
    {
        public SaveEncounterResult(SaveEncounterStatus status, EncounterNote? note = null)
        {
            Status = status;
            Note = note;
        }

        public SaveEncounterStatus Status { get; }
        public EncounterNote? Note { get; }
    }
}
