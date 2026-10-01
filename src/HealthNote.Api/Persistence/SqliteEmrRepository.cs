using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;
using Microsoft.Data.Sqlite;

namespace HealthNote.Api.Persistence
{
    public sealed class SqliteEmrRepository : IEmrRepository
    {
        private readonly string _connectionString;

        public SqliteEmrRepository(string path)
        {
            string absolute = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? throw new ArgumentException("Invalid DB path."));
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = absolute, ForeignKeys = true, Pooling = false, DefaultTimeout = 10
            }.ToString();
            Initialize();
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void Initialize()
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(deferred: false);
            using var schemaVersion = Command(connection, transaction, "PRAGMA user_version");
            long version = Convert.ToInt64(schemaVersion.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (version != 0 && version != 1) throw new InvalidOperationException("Unsupported EMR database schema.");
            using var schema = Command(connection, transaction, """
                CREATE TABLE IF NOT EXISTS patients (
                    id TEXT PRIMARY KEY, patient_number TEXT NOT NULL UNIQUE, display_name TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS encounters (
                    id TEXT PRIMARY KEY, patient_id TEXT NOT NULL REFERENCES patients(id),
                    visit_date TEXT NOT NULL, chief_complaint TEXT NOT NULL,
                    assessment TEXT NOT NULL, plan TEXT NOT NULL, version INTEGER NOT NULL CHECK(version > 0),
                    saved_at_utc TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS encounter_patient_date ON encounters(patient_id, visit_date DESC, id);
                PRAGMA user_version=1;
                """);
            schema.ExecuteNonQuery();
            string[] names = { "합성환자 가", "합성환자 나", "합성환자 다" };
            for (int i = 0; i < names.Length; i++)
            {
                using var seed = Command(connection, transaction,
                    "INSERT OR IGNORE INTO patients VALUES ($id,$number,$name)",
                    ("$id", $"00000000-0000-0000-0000-{i + 1:000000000000}"),
                    ("$number", $"DEMO-{i + 1:000}"), ("$name", names[i]));
                seed.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        public Page<Patient> GetPatients(string search, int page, int pageSize)
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(deferred: true);
            const string filter = "instr(display_name,$search)>0 OR instr(patient_number,$search)>0";
            using var count = Command(connection, transaction, "SELECT COUNT(*) FROM patients WHERE " + filter, ("$search", search));
            long total = Convert.ToInt64(count.ExecuteScalar(), CultureInfo.InvariantCulture);
            using var query = Command(connection, transaction,
                "SELECT id,patient_number,display_name FROM patients WHERE " + filter + " ORDER BY patient_number LIMIT $limit OFFSET $offset",
                ("$search", search), ("$limit", pageSize), ("$offset", ((long)page - 1) * pageSize));
            var items = new List<Patient>();
            using (var reader = query.ExecuteReader())
                while (reader.Read()) items.Add(ReadPatient(reader));
            transaction.Commit();
            return new Page<Patient>(items, total, page, pageSize);
        }

        public Patient? GetPatient(Guid patientId)
        {
            using var connection = Open();
            using var query = Command(connection, null, "SELECT id,patient_number,display_name FROM patients WHERE id=$id", ("$id", patientId.ToString("D")));
            using var reader = query.ExecuteReader();
            return reader.Read() ? ReadPatient(reader) : null;
        }

        public Page<EncounterNote> GetEncounters(Guid patientId, int page, int pageSize)
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(deferred: true);
            using var count = Command(connection, transaction, "SELECT COUNT(*) FROM encounters WHERE patient_id=$patient", ("$patient", patientId.ToString("D")));
            long total = Convert.ToInt64(count.ExecuteScalar(), CultureInfo.InvariantCulture);
            using var query = Command(connection, transaction, """
                SELECT * FROM encounters WHERE patient_id=$patient
                ORDER BY visit_date DESC,id ASC LIMIT $limit OFFSET $offset
                """, ("$patient", patientId.ToString("D")), ("$limit", pageSize), ("$offset", ((long)page - 1) * pageSize));
            var items = new List<EncounterNote>();
            using (var reader = query.ExecuteReader())
                while (reader.Read()) items.Add(ReadNote(reader));
            transaction.Commit();
            return new Page<EncounterNote>(items, total, page, pageSize);
        }

        public EncounterNote? GetEncounter(Guid patientId, Guid id)
        {
            using var connection = Open();
            using var query = Command(connection, null, "SELECT * FROM encounters WHERE id=$id AND patient_id=$patient",
                ("$id", id.ToString("D")), ("$patient", patientId.ToString("D")));
            using var reader = query.ExecuteReader();
            return reader.Read() ? ReadNote(reader) : null;
        }

        public SaveEncounterResult Save(Guid patientId, Guid id, EncounterContent content, int expectedVersion, DateTime savedAtUtc)
        {
            using var connection = Open();
            // Acquire the write lock before checking ownership/version; concurrent writers cannot both pass.
            using var transaction = connection.BeginTransaction(deferred: false);
            using var patient = Command(connection, transaction, "SELECT COUNT(*) FROM patients WHERE id=$id", ("$id", patientId.ToString("D")));
            if (Convert.ToInt64(patient.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                return new SaveEncounterResult(SaveEncounterStatus.NotFound);
            int actualVersion = 0;
            using (var existing = Command(connection, transaction, "SELECT patient_id,version FROM encounters WHERE id=$id", ("$id", id.ToString("D"))))
            using (var reader = existing.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (reader.GetString(0) != patientId.ToString("D")) return new SaveEncounterResult(SaveEncounterStatus.NotFound);
                    actualVersion = reader.GetInt32(1);
                }
            }
            if (expectedVersion != actualVersion) return new SaveEncounterResult(SaveEncounterStatus.Conflict);
            var note = new EncounterNote(id, patientId, content, checked(actualVersion + 1), savedAtUtc);
            string sql = actualVersion == 0
                ? "INSERT INTO encounters VALUES ($id,$patient,$date,$chief,$assessment,$plan,$version,$saved)"
                : "UPDATE encounters SET visit_date=$date,chief_complaint=$chief,assessment=$assessment,plan=$plan,version=$version,saved_at_utc=$saved WHERE id=$id AND patient_id=$patient";
            using var write = Command(connection, transaction, sql,
                ("$id", id.ToString("D")), ("$patient", patientId.ToString("D")),
                ("$date", content.VisitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$chief", content.ChiefComplaint), ("$assessment", content.Assessment), ("$plan", content.Plan),
                ("$version", note.Version), ("$saved", savedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            write.ExecuteNonQuery();
            transaction.Commit();
            return new SaveEncounterResult(actualVersion == 0 ? SaveEncounterStatus.Created : SaveEncounterStatus.Updated, note);
        }

        private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Key, object Value)[] parameters)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            return command;
        }

        private static Patient ReadPatient(SqliteDataReader reader) =>
            new Patient(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2));

        private static EncounterNote ReadNote(SqliteDataReader reader) =>
            new EncounterNote(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
                new EncounterContent(DateTime.ParseExact(reader.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5)),
                reader.GetInt32(6), DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}
