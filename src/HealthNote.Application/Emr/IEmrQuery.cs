using System;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Domain.Emr;

namespace HealthNote.Application.Emr
{
    public interface IEmrQuery
    {
        Task<Page<Patient>> GetPatientsAsync(string search, int page, int pageSize, CancellationToken cancellationToken);
        Task<Page<EncounterSummary>> GetEncountersAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken);
        Task<EncounterNote> GetEncounterAsync(Guid patientId, Guid id, CancellationToken cancellationToken);
    }
}
