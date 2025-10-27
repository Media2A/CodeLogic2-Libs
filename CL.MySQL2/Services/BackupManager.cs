using CodeLogic.Abstractions;
using MySqlConnector;
using System.Text;

namespace CL.MySQL2.Services;

/// <summary>
/// Manages SQL schema backups for database tables before synchronization.
/// </summary>
public class BackupManager
{
    private readonly string _backupDirectory;
    private readonly ILogger? _logger;
    private const string BackupFolderName = "backups";

    public BackupManager(string dataDirectory, ILogger? logger = null)
    {
        _logger = logger;
        _backupDirectory = Path.Combine(dataDirectory, BackupFolderName);
        EnsureBackupDirectoryExists();
    }

    /// <summary>
    /// Ensures the backup directory exists.
    /// </summary>
    private void EnsureBackupDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                _logger?.Info($"Created backup directory: {_backupDirectory}");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create backup directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a backup of the table schema before synchronization.
    /// </summary>
    public async Task<bool> BackupTableSchemaAsync(
        MySqlConnection connection,
        string tableName,
        string connectionId = "Default")
    {
        try
        {
            _logger?.Info($"Backing up schema for table '{tableName}'");

            // Get the CREATE TABLE statement
            using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{tableName}`", connection);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                _logger?.Warning($"Could not retrieve CREATE TABLE statement for '{tableName}'");
                return false;
            }

            var createTableStatement = reader.GetString(1);

            // Generate backup file name
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var backupFileName = $"{timestamp}_{tableName}_backup.sql";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            // Write backup file
            var sb = new StringBuilder();
            sb.AppendLine("-- MySQL Table Schema Backup");
            sb.AppendLine($"-- Table: {tableName}");
            sb.AppendLine($"-- Connection: {connectionId}");
            sb.AppendLine($"-- Timestamp: {DateTime.UtcNow:O}");
            sb.AppendLine("-- WARNING: This is a schema backup only, not a data backup");
            sb.AppendLine();
            sb.AppendLine(createTableStatement);
            sb.AppendLine(";");

            await File.WriteAllTextAsync(backupFilePath, sb.ToString(), Encoding.UTF8);

            _logger?.Info($"Schema backup created: {backupFileName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to backup table schema for '{tableName}': {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Creates a backup of the entire database schema.
    /// </summary>
    public async Task<bool> BackupDatabaseSchemaAsync(
        MySqlConnection connection,
        string? tablesToBackup = null,
        string connectionId = "Default")
    {
        try
        {
            _logger?.Info("Backing up database schema");

            // Get all tables if not specified
            var tables = new List<string>();
            if (string.IsNullOrEmpty(tablesToBackup))
            {
                using var cmd = new MySqlCommand(
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
                    connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            else
            {
                tables = tablesToBackup.Split(',').Select(t => t.Trim()).ToList();
            }

            if (!tables.Any())
            {
                _logger?.Warning("No tables found for backup");
                return false;
            }

            // Generate backup file name
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var backupFileName = $"{timestamp}_full_database_backup.sql";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            // Build backup content
            var sb = new StringBuilder();
            sb.AppendLine("-- MySQL Full Database Schema Backup");
            sb.AppendLine($"-- Connection: {connectionId}");
            sb.AppendLine($"-- Timestamp: {DateTime.UtcNow:O}");
            sb.AppendLine($"-- Tables: {string.Join(", ", tables)}");
            sb.AppendLine("-- WARNING: This is a schema backup only, not a data backup");
            sb.AppendLine();

            // Get CREATE TABLE statements for each table
            foreach (var table in tables)
            {
                try
                {
                    using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", connection);
                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var createTableStatement = reader.GetString(1);
                        sb.AppendLine($"-- Table: {table}");
                        sb.AppendLine(createTableStatement);
                        sb.AppendLine(";");
                        sb.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to backup table '{table}': {ex.Message}");
                }
            }

            await File.WriteAllTextAsync(backupFilePath, sb.ToString(), Encoding.UTF8);

            _logger?.Info($"Full database schema backup created: {backupFileName} ({tables.Count} tables)");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to backup database schema: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets a list of existing backup files.
    /// </summary>
    public List<FileInfo> GetBackupFiles(string? tableNameFilter = null)
    {
        try
        {
            var directory = new DirectoryInfo(_backupDirectory);

            if (!directory.Exists)
                return new List<FileInfo>();

            var files = directory.GetFiles("*.sql").OrderByDescending(f => f.CreationTime).ToList();

            if (!string.IsNullOrEmpty(tableNameFilter))
            {
                files = files.Where(f => f.Name.Contains(tableNameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to get backup files: {ex.Message}", ex);
            return new List<FileInfo>();
        }
    }

    /// <summary>
    /// Deletes old backup files, keeping only the most recent ones.
    /// </summary>
    public async Task<int> CleanupOldBackupsAsync(int keepCount = 10, int? maxAgeInDays = null)
    {
        try
        {
            var backupFiles = GetBackupFiles();

            if (backupFiles.Count <= keepCount)
            {
                _logger?.Debug($"Backup cleanup: {backupFiles.Count} files found, keeping {keepCount}. No cleanup needed.");
                return 0;
            }

            var filesToDelete = new List<FileInfo>();

            // Remove old backups by count
            for (int i = keepCount; i < backupFiles.Count; i++)
            {
                filesToDelete.Add(backupFiles[i]);
            }

            // Also remove backups older than specified days
            if (maxAgeInDays.HasValue && maxAgeInDays > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
                filesToDelete = filesToDelete.Union(
                    backupFiles.Where(f => f.CreationTime < cutoffDate)
                ).ToList();
            }

            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                    _logger?.Debug($"Deleted old backup file: {file.Name}");
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to delete backup file '{file.Name}': {ex.Message}");
                }
            }

            if (filesToDelete.Any())
            {
                _logger?.Info($"Cleaned up {filesToDelete.Count} old backup file(s)");
            }

            return filesToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to cleanup old backups: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Gets the path to the backup directory.
    /// </summary>
    public string GetBackupDirectoryPath() => _backupDirectory;
}
