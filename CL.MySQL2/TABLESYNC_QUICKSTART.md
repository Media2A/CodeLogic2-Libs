# TableSync - Quick Start Guide

## 5-Minute Setup

### Step 1: Create Your Model

```csharp
using CL.MySQL2.Models;

[Table(Name = "users", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class User
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long UserId { get; set; }

    [Column(DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true)]
    public string Email { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string FullName { get; set; }

    [Column(DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(DataType = DataType.Bool, DefaultValue = "1")]
    public bool IsActive { get; set; }
}
```

### Step 2: Sync the Table

```csharp
// In your startup code or service initialization
var mysql2 = serviceProvider.GetRequiredService<MySQL2Library>();

// Sync the table (creates if doesn't exist, updates if does)
bool success = await mysql2.SyncTableAsync<User>(
    connectionId: "Default",
    createBackup: true  // Recommended for production
);

if (success)
    Console.WriteLine("✓ User table synchronized successfully!");
```

### Step 3: Done! ✓

The table is now created and synchronized with your model definition.

---

## Common Operations

### Sync Multiple Tables at Once

```csharp
var results = await mysql2.SyncTablesAsync(
    new Type[] { typeof(User), typeof(Order), typeof(Product) },
    connectionId: "Default"
);

foreach (var (table, success) in results)
{
    Console.WriteLine($"{table}: {(success ? "✓" : "✗")}");
}
```

### Access Sync Service Directly

```csharp
var syncService = mysql2.GetTableSyncService();

// Get migration tracker
var migrations = syncService.GetMigrationTracker();
var history = await migrations.GetTableMigrationsAsync("users");

// Get backup manager
var backups = syncService.GetBackupManager();
var backupFiles = backups.GetBackupFiles();
```

### Check Migration History

```csharp
var tracker = syncService.GetMigrationTracker();

// View all migrations for a table
var migrations = await tracker.GetTableMigrationsAsync("users");
foreach (var m in migrations)
{
    Console.WriteLine($"{m.AppliedAt:O} - {m.MigrationType}: {m.Description}");
}

// Check for failures
var failed = await tracker.GetFailedMigrationsAsync();
if (failed.Any())
{
    Console.WriteLine($"⚠ {failed.Count} migration(s) failed!");
}
```

### View Backups

```csharp
var backups = syncService.GetBackupManager();

// List all backups
var files = backups.GetBackupFiles();
foreach (var f in files)
{
    Console.WriteLine($"{f.Name} - {f.CreationTime}");
}

// Cleanup old backups (keep 10, max 60 days old)
int deleted = await backups.CleanupOldBackupsAsync(keepCount: 10, maxAgeInDays: 60);
Console.WriteLine($"Deleted {deleted} old backup files");
```

---

## Column Attribute Cheat Sheet

### Required / Key Columns

```csharp
// Primary key (auto-increment)
[Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
public long Id { get; set; }

// Required text
[Column(DataType = DataType.VarChar, Size = 100, NotNull = true)]
public string Name { get; set; }

// Unique email
[Column(DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true)]
public string Email { get; set; }

// Indexed column
[Column(DataType = DataType.VarChar, Size = 50, Index = true)]
public string Status { get; set; }
```

### Data Type Examples

```csharp
// Integers
[Column(DataType = DataType.Int)]
[Column(DataType = DataType.BigInt)]
[Column(DataType = DataType.SmallInt)]

// Decimals/Money
[Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)]  // Price: 99999.99

// Strings
[Column(DataType = DataType.VarChar, Size = 50)]    // Short strings
[Column(DataType = DataType.VarChar, Size = 255)]   // URLs, descriptions
[Column(DataType = DataType.Text)]                  // Long content
[Column(DataType = DataType.LongText)]              // Very long content

// Dates
[Column(DataType = DataType.Date)]                  // Date only
[Column(DataType = DataType.DateTime)]              // Date and time
[Column(DataType = DataType.Timestamp)]             // Auto-updating

// JSON
[Column(DataType = DataType.Json)]                  // Structured data

// Boolean
[Column(DataType = DataType.Bool)]                  // True/False
```

### Default Values

```csharp
// Current timestamp
[Column(DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
public DateTime CreatedAt { get; set; }

// Auto-update timestamp
[Column(DataType = DataType.Timestamp,
        DefaultValue = "CURRENT_TIMESTAMP",
        OnUpdateCurrentTimestamp = true)]
public DateTime UpdatedAt { get; set; }

// Boolean default
[Column(DataType = DataType.Bool, DefaultValue = "1")]
public bool IsActive { get; set; }

// String default
[Column(DataType = DataType.VarChar, Size = 50, DefaultValue = "PENDING")]
public string Status { get; set; }

// NULL default
[Column(DataType = DataType.Text, DefaultValue = "NULL")]
public string? Notes { get; set; }
```

---

## Directory Structure

After first sync, you'll have:

```
data/
├── backups/
│   ├── 2025-10-27_12-30-45_users_backup.sql
│   └── 2025-10-27_12-31-22_orders_backup.sql
├── migrations/
│   └── migration_history.json
└── cl.mysql2/

logs/
└── (TableSync logs appear here)
```

---

## Configuration

Enable in `config/mysql.json`:

```json
{
  "Default": {
    "host": "localhost",
    "port": 3306,
    "database": "my_app",
    "username": "root",
    "password": "",
    "enable_auto_sync": true,
    "enable_logging": true
  }
}
```

---

## Logging

All operations are logged automatically:

```
[INFO] Starting table synchronization for model 'User'
[INFO] Table 'users' does not exist. Creating it now.
[INFO] Successfully created table 'users'
[INFO] Recorded migration: users - CREATE - SUCCESS
```

Check logs in the `logs/` directory.

---

## Troubleshooting

### Table not created?
1. Check database credentials in `config/mysql.json`
2. Verify user has CREATE TABLE permission
3. Check logs for specific error messages

### Changes not syncing?
1. Verify `enable_auto_sync` is true
2. Check that model attributes are correct
3. Look for failed migrations in history

### Backup directory not found?
1. Ensure `data/` directory exists
2. Check file permissions
3. Review logs for creation errors

---

## Next Steps

- Read **TABLESYNC_GUIDE.md** for comprehensive documentation
- Check examples in **EXAMPLES.md** (if available)
- Review API in service files for advanced usage
- Monitor migrations and backups regularly

---

**Version:** 2.0.0
**Framework:** CodeLogic2 + CL.MySQL2
**Last Updated:** 2025-10-27
