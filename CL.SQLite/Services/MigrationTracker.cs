using CodeLogic.Abstractions;
using System.Text.Json;

namespace CL.SQLite.Services;

/// <summary>
/// Tracks and manages database schema migrations to prevent duplicate operations.
/// </summary>
public class MigrationTracker
{
    private readonly string _migrationsDirectory;
    private readonly ILogger? _logger;
    private const string MigrationsFolderName = "migrations";
    private const string MigrationsHistoryFile = "migration_history.json";

    public MigrationTracker(string dataDirectory, ILogger? logger = null)
    {
        _logger = logger;
        _migrationsDirectory = Path.Combine(dataDirectory, MigrationsFolderName);
        EnsureMigrationsDirectoryExists();
    }

    /// <summary>
    /// Represents a migration record.
    /// </summary>
    public class MigrationRecord
    {
        public string Id { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string MigrationType { get; set; } = string.Empty; // "CREATE", "ALTER", "DROP", etc.
        public DateTime AppliedAt { get; set; }
        public string? Description { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MigrationSql { get; set; }
    }

    /// <summary>
    /// Ensures the migrations directory exists.
    /// </summary>
    private void EnsureMigrationsDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_migrationsDirectory))
            {
                Directory.CreateDirectory(_migrationsDirectory);
                _logger?.Info($"Created migrations directory: {_migrationsDirectory}");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create migrations directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Records a migration operation.
    /// </summary>
    public async Task<bool> RecordMigrationAsync(
        string tableName,
        string migrationType,
        string? description = null,
        bool success = true,
        string? errorMessage = null,
        string? migrationSql = null)
    {
        try
        {
            var migration = new MigrationRecord
            {
                Id = GenerateMigrationId(tableName, migrationType),
                TableName = tableName,
                MigrationType = migrationType,
                AppliedAt = DateTime.UtcNow,
                Description = description,
                Success = success,
                ErrorMessage = errorMessage,
                MigrationSql = migrationSql
            };

            var history = await LoadMigrationHistoryAsync();
            history.Add(migration);

            await SaveMigrationHistoryAsync(history);

            _logger?.Info($"Recorded migration: {tableName} - {migrationType} - {(success ? "SUCCESS" : "FAILED")}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to record migration: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Checks if a migration has already been applied.
    /// </summary>
    public async Task<bool> MigrationExistsAsync(string tableName, string migrationType)
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();
            var exists = history.Any(m =>
                m.TableName == tableName &&
                m.MigrationType == migrationType &&
                m.Success);

            return exists;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to check if migration exists: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets all migrations for a specific table.
    /// </summary>
    public async Task<List<MigrationRecord>> GetTableMigrationsAsync(string tableName)
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();
            return history.Where(m => m.TableName == tableName).OrderByDescending(m => m.AppliedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to get migrations for table '{tableName}': {ex.Message}", ex);
            return new List<MigrationRecord>();
        }
    }

    /// <summary>
    /// Gets all failed migrations.
    /// </summary>
    public async Task<List<MigrationRecord>> GetFailedMigrationsAsync()
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();
            return history.Where(m => !m.Success).OrderByDescending(m => m.AppliedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to get failed migrations: {ex.Message}", ex);
            return new List<MigrationRecord>();
        }
    }

    /// <summary>
    /// Gets the complete migration history.
    /// </summary>
    public async Task<List<MigrationRecord>> GetMigrationHistoryAsync()
    {
        return await LoadMigrationHistoryAsync();
    }

    /// <summary>
    /// Gets migration statistics.
    /// </summary>
    public async Task<MigrationStatistics> GetStatisticsAsync()
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();
            var now = DateTime.UtcNow;

            return new MigrationStatistics
            {
                TotalMigrations = history.Count,
                SuccessfulMigrations = history.Count(m => m.Success),
                FailedMigrations = history.Count(m => !m.Success),
                UniqueTables = history.Select(m => m.TableName).Distinct().Count(),
                FirstMigration = history.OrderBy(m => m.AppliedAt).FirstOrDefault()?.AppliedAt,
                LastMigration = history.OrderByDescending(m => m.AppliedAt).FirstOrDefault()?.AppliedAt,
                MigrationsByType = history
                    .GroupBy(m => m.MigrationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentMigrations = history.Where(m => now - m.AppliedAt <= TimeSpan.FromHours(24)).Count()
            };
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to get migration statistics: {ex.Message}", ex);
            return new MigrationStatistics();
        }
    }

    /// <summary>
    /// Clears old migration records, keeping only recent ones.
    /// </summary>
    public async Task<int> CleanupOldMigrationsAsync(int keepCount = 100, int? maxAgeInDays = null)
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();

            if (history.Count <= keepCount)
            {
                _logger?.Debug($"Migration cleanup: {history.Count} records found, keeping {keepCount}. No cleanup needed.");
                return 0;
            }

            var sortedHistory = history.OrderByDescending(m => m.AppliedAt).ToList();
            var recordsToKeep = sortedHistory.Take(keepCount).ToList();

            // Also consider age filter
            if (maxAgeInDays.HasValue && maxAgeInDays > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
                recordsToKeep = recordsToKeep.Where(m => m.AppliedAt >= cutoffDate).ToList();
            }

            var removedCount = history.Count - recordsToKeep.Count;

            if (removedCount > 0)
            {
                await SaveMigrationHistoryAsync(recordsToKeep);
                _logger?.Info($"Cleaned up {removedCount} old migration record(s)");
            }

            return removedCount;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to cleanup old migrations: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Exports migration history to a file.
    /// </summary>
    public async Task<bool> ExportHistoryAsync(string filePath)
    {
        try
        {
            var history = await LoadMigrationHistoryAsync();
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            _logger?.Info($"Exported migration history to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to export migration history: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Imports migration history from a file.
    /// </summary>
    public async Task<bool> ImportHistoryAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger?.Warning($"Migration history file not found: {filePath}");
                return false;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var records = JsonSerializer.Deserialize<List<MigrationRecord>>(json) ?? new List<MigrationRecord>();

            await SaveMigrationHistoryAsync(records);
            _logger?.Info($"Imported {records.Count} migration record(s) from: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to import migration history: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Loads migration history from file.
    /// </summary>
    private async Task<List<MigrationRecord>> LoadMigrationHistoryAsync()
    {
        var historyFile = Path.Combine(_migrationsDirectory, MigrationsHistoryFile);

        if (!File.Exists(historyFile))
            return new List<MigrationRecord>();

        try
        {
            var json = await File.ReadAllTextAsync(historyFile);
            var records = JsonSerializer.Deserialize<List<MigrationRecord>>(json);
            return records ?? new List<MigrationRecord>();
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to load migration history: {ex.Message}", ex);
            return new List<MigrationRecord>();
        }
    }

    /// <summary>
    /// Saves migration history to file.
    /// </summary>
    private async Task SaveMigrationHistoryAsync(List<MigrationRecord> history)
    {
        var historyFile = Path.Combine(_migrationsDirectory, MigrationsHistoryFile);

        try
        {
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(historyFile, json);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to save migration history: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a unique migration ID.
    /// </summary>
    private string GenerateMigrationId(string tableName, string migrationType)
    {
        return $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{tableName}_{migrationType}";
    }

    /// <summary>
    /// Gets the path to the migrations directory.
    /// </summary>
    public string GetMigrationsDirectoryPath() => _migrationsDirectory;
}

/// <summary>
/// Migration statistics summary.
/// </summary>
public class MigrationStatistics
{
    public int TotalMigrations { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public int UniqueTables { get; set; }
    public DateTime? FirstMigration { get; set; }
    public DateTime? LastMigration { get; set; }
    public Dictionary<string, int> MigrationsByType { get; set; } = new();
    public int RecentMigrations { get; set; }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Migration Statistics ===");
        sb.AppendLine($"Total Migrations: {TotalMigrations}");
        sb.AppendLine($"Successful: {SuccessfulMigrations}");
        sb.AppendLine($"Failed: {FailedMigrations}");
        sb.AppendLine($"Unique Tables: {UniqueTables}");
        sb.AppendLine($"First Migration: {FirstMigration:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Last Migration: {LastMigration:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Recent (24h): {RecentMigrations}");

        if (MigrationsByType.Any())
        {
            sb.AppendLine("\nBy Type:");
            foreach (var (type, count) in MigrationsByType.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  {type}: {count}");
            }
        }

        return sb.ToString();
    }
}
