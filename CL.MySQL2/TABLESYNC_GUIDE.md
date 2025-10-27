# TableSync - Schema Synchronization Guide

## Overview

`TableSync` is a comprehensive schema synchronization system for CL.MySQL2 that automatically synchronizes C# model definitions with MySQL database tables. It includes advanced features for migrations tracking, automated backups, and detailed schema analysis.

## Features

- ✅ **Automatic Table Creation** - Creates tables from model definitions
- ✅ **Schema Synchronization** - Syncs columns, indexes, primary keys, and table engines
- ✅ **Migration Tracking** - Automatically tracks all schema changes with history
- ✅ **Automated Backups** - Creates schema backups before major changes
- ✅ **Comprehensive Logging** - Uses CodeLogic framework logging throughout
- ✅ **Batch Operations** - Synchronize multiple tables at once
- ✅ **Index Management** - Creates and maintains indexes automatically

## Directory Structure

TableSync uses the framework's standard directories:

```
data/
├── backups/           # SQL schema backup files (*.sql)
│   ├── 2025-10-27_12-30-45_users_backup.sql
│   ├── 2025-10-27_13-45-22_orders_backup.sql
│   └── 2025-10-27_14-20-15_full_database_backup.sql
├── migrations/        # Migration tracking directory
│   └── migration_history.json
└── cl.mysql2/         # Other library data

logs/
└── (TableSync logs automatically go here via ILogger)
```

## Basic Usage

### 1. Define Your Model with Attributes

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

    [Column(DataType = DataType.DateTime,
            DefaultValue = "CURRENT_TIMESTAMP",
            OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }

    [Column(DataType = DataType.Text, NotNull = false)]
    public string? Biography { get; set; }

    [Column(DataType = DataType.Bool, DefaultValue = "1")]
    public bool IsActive { get; set; }
}
```

### 2. Sync a Single Table

```csharp
// Get the library from CodeLogic framework
var mysql2 = serviceProvider.GetRequiredService<MySQL2Library>();

// Sync the User table with the database
bool success = await mysql2.SyncTableAsync<User>(
    connectionId: "Default",
    createBackup: true  // Creates schema backup before changes
);

if (success)
{
    Console.WriteLine("User table synchronized successfully!");
}
```

### 3. Sync Multiple Tables at Once

```csharp
// Sync multiple tables in a single operation
var result = await mysql2.SyncTablesAsync(
    modelTypes: new Type[] { typeof(User), typeof(Order), typeof(Product) },
    connectionId: "Default",
    createBackup: true
);

// Check results
foreach (var kvp in result)
{
    Console.WriteLine($"{kvp.Key}: {(kvp.Value ? "✓ Success" : "✗ Failed")}");
}
```

## Column Attributes

### ColumnAttribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataType` | DataType | (required) | MySQL data type (Int, VarChar, DateTime, etc.) |
| `Name` | string? | null | Custom column name (uses property name if null) |
| `Size` | int | 0 | Size for VARCHAR, CHAR, etc. (255 if not set for VARCHAR) |
| `Precision` | int | 10 | Precision for DECIMAL type |
| `Scale` | int | 2 | Scale for DECIMAL type |
| `Primary` | bool | false | Mark as primary key |
| `AutoIncrement` | bool | false | Auto-increment for integer types |
| `NotNull` | bool | false | NOT NULL constraint |
| `Unique` | bool | false | UNIQUE constraint |
| `Index` | bool | false | Create an index |
| `Unsigned` | bool | false | UNSIGNED for numeric types |
| `DefaultValue` | string? | null | Default value (use "CURRENT_TIMESTAMP" for timestamps) |
| `Charset` | Charset? | null | Override table charset |
| `OnUpdateCurrentTimestamp` | bool | false | Auto-update on row modification (TIMESTAMP only) |
| `Comment` | string? | null | Column comment |

### Example Column Definitions

```csharp
// Primary Key Auto-Increment
[Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
public long Id { get; set; }

// Indexed VarChar with unique constraint
[Column(DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
public string Email { get; set; }

// DECIMAL for money
[Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)]
public decimal Price { get; set; }

// Timestamp with auto-update
[Column(DataType = DataType.Timestamp,
        DefaultValue = "CURRENT_TIMESTAMP",
        OnUpdateCurrentTimestamp = true)]
public DateTime UpdatedAt { get; set; }

// Optional nullable column
[Column(DataType = DataType.Text, NotNull = false)]
[Ignore]  // Exclude from database
public string? Notes { get; set; }
```

## Table Attributes

### TableAttribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | string? | null | Custom table name (uses class name if null) |
| `Engine` | TableEngine | InnoDB | Storage engine (InnoDB, MyISAM, Memory, Archive, CSV) |
| `Charset` | Charset | Utf8mb4 | Default character set |
| `Collation` | string? | null | Database collation |
| `Comment` | string? | null | Table comment |

### Composite Indexes

Create indexes on multiple columns:

```csharp
[CompositeIndex("idx_user_email_status", "Email", "IsActive", Unique = false)]
public class User
{
    [Column(DataType = DataType.VarChar, Size = 100, NotNull = true)]
    public string Email { get; set; }

    [Column(DataType = DataType.Bool)]
    public bool IsActive { get; set; }
}
```

## Supported Data Types

| C# DataType | MySQL Type | Notes |
|-------------|-----------|-------|
| TinyInt | TINYINT | -128 to 127 or 0 to 255 |
| SmallInt | SMALLINT | Small integers |
| MediumInt | MEDIUMINT | Medium integers |
| Int | INT | Standard 32-bit integers |
| BigInt | BIGINT | 64-bit integers |
| Float | FLOAT | Single-precision floating point |
| Double | DOUBLE | Double-precision floating point |
| Decimal | DECIMAL(p,s) | Fixed-point, use Precision and Scale |
| DateTime | DATETIME | Date and time |
| Date | DATE | Date only |
| Time | TIME | Time only |
| Timestamp | TIMESTAMP | Auto-updating timestamp |
| Year | YEAR | 4-digit year |
| Char | CHAR(n) | Fixed-length string |
| VarChar | VARCHAR(n) | Variable-length string |
| TinyText | TINYTEXT | 255 bytes max |
| Text | TEXT | 65KB max |
| MediumText | MEDIUMTEXT | 16MB max |
| LongText | LONGTEXT | 4GB max |
| Json | JSON | JSON-encoded text |
| Binary | BINARY(n) | Fixed-length binary |
| VarBinary | VARBINARY(n) | Variable-length binary |
| TinyBlob | TINYBLOB | 255 bytes binary |
| Blob | BLOB | 65KB binary |
| MediumBlob | MEDIUMBLOB | 16MB binary |
| LongBlob | LONGBLOB | 4GB binary |
| Uuid | CHAR(36) | UUID/GUID storage |
| Enum | ENUM | Enumeration |
| Set | SET | Multiple values |
| Bool | TINYINT(1) | Boolean (0 or 1) |

## Migration Tracking

TableSync automatically tracks all migrations. Access migration history programmatically:

```csharp
var mysql2 = serviceProvider.GetRequiredService<MySQL2Library>();
var syncService = mysql2.GetTableSyncService();
var migrationTracker = syncService.GetMigrationTracker();

// Get all migrations for a specific table
var userMigrations = await migrationTracker.GetTableMigrationsAsync("users");
foreach (var migration in userMigrations)
{
    Console.WriteLine($"{migration.AppliedAt:O} - {migration.MigrationType}: {migration.Description}");
}

// Get all migrations for a connection
var allMigrations = await migrationTracker.GetConnectionMigrationsAsync("Default");

// Get failed migrations
var failedMigrations = await migrationTracker.GetFailedMigrationsAsync();

// Export migration history
await migrationTracker.ExportHistoryAsync("migration_export.json");

// Cleanup old migrations (keep 100, max 30 days old)
int removed = await migrationTracker.CleanupOldMigrationsAsync(keepCount: 100, maxAgeInDays: 30);
```

## Backup Management

TableSync automatically creates schema backups before major changes. You can also manage backups manually:

```csharp
var syncService = mysql2.GetTableSyncService();
var backupManager = syncService.GetBackupManager();

// Create manual table schema backup
bool success = await backupManager.BackupTableSchemaAsync(
    connection,
    "users",
    "Default"
);

// Create full database schema backup
success = await backupManager.BackupDatabaseSchemaAsync(
    connection,
    "users,orders,products",  // Specific tables, or null for all
    "Default"
);

// List existing backups
var backups = backupManager.GetBackupFiles();
foreach (var backup in backups)
{
    Console.WriteLine($"{backup.Name} - {backup.CreationTime:O}");
}

// Filter backups for specific table
var userBackups = backupManager.GetBackupFiles("users");

// Cleanup old backups (keep 10, max 60 days old)
int deleted = await backupManager.CleanupOldBackupsAsync(keepCount: 10, maxAgeInDays: 60);

// Get backup directory path
string backupDir = backupManager.GetBackupDirectoryPath();
```

## Advanced Usage

### Foreign Keys

While not yet fully integrated, foreign keys can be defined via attributes:

```csharp
[Column(DataType = DataType.BigInt)]
[ForeignKey(
    ReferenceTable = "users",
    ReferenceColumn = "UserId",
    OnDelete = ForeignKeyAction.Cascade,
    OnUpdate = ForeignKeyAction.Cascade
)]
public long UserId { get; set; }
```

### Ignoring Properties

Use the `IgnoreAttribute` to exclude properties from database schema:

```csharp
[Ignore]
public string? CalculatedField { get; set; }
```

### Schema Analysis

The `SchemaAnalyzer` provides detailed schema comparison:

```csharp
var analyzer = new SchemaAnalyzer(_logger);

// Generate model column definitions
var modelColumns = analyzer.GenerateModelColumnDefinitions(typeof(User));

// Get database columns
var dbColumns = await analyzer.GetDatabaseColumnsAsync(connection, "users");

// Check table existence
bool exists = await analyzer.TableExistsAsync(connection, "users");

// Get table indexes
var indexes = await analyzer.GetTableIndexesAsync(connection, "users");
foreach (var (name, isUnique, columns) in indexes)
{
    Console.WriteLine($"{name}: {string.Join(", ", columns)} (Unique: {isUnique})");
}

// Generate CREATE TABLE statement
string sql = analyzer.GenerateCreateTableStatement("users", modelColumns);
```

## Logging Integration

All TableSync operations are logged through the CodeLogic framework's ILogger interface. Logs appear in the configured logs directory with full context:

```
[INFO] 2025-10-27 12:30:45.123 - Starting table synchronization for model 'User' (table: 'users')
[DEBUG] 2025-10-27 12:30:45.456 - Table 'users' does not exist. Creating it now.
[INFO] 2025-10-27 12:30:45.789 - Successfully created table 'users'
[INFO] 2025-10-27 12:30:46.012 - Creating indexes for new table 'users'
[INFO] 2025-10-27 12:30:46.345 - Recorded migration: users - CREATE - SUCCESS
```

## Configuration

Enable or disable auto-sync in `config/mysql.json`:

```json
{
  "Default": {
    "host": "localhost",
    "port": 3306,
    "database": "my_app",
    "username": "root",
    "password": "",
    "enable_logging": true,
    "enable_auto_sync": true
  }
}
```

When `enable_auto_sync` is true, tables are automatically synchronized on library initialization.

## Best Practices

1. **Always Use Descriptive Names**
   ```csharp
   [Table(Name = "users", Comment = "Application users table")]
   public class User { }
   ```

2. **Set Appropriate Sizes**
   ```csharp
   [Column(DataType = DataType.VarChar, Size = 100)]  // Email
   [Column(DataType = DataType.VarChar, Size = 255)]  // URL
   [Column(DataType = DataType.Text)]                 // Long content
   ```

3. **Use Correct Data Types**
   ```csharp
   [Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)]  // Prices
   [Column(DataType = DataType.DateTime)]  // Timestamps
   [Column(DataType = DataType.Json)]      // Structured data
   ```

4. **Mark Constraints Clearly**
   ```csharp
   [Column(Primary = true, AutoIncrement = true)]     // Primary key
   [Column(NotNull = true, Unique = true, Index = true)]  // Unique indexed field
   [Column(NotNull = true)]                           // Required field
   ```

5. **Use Meaningful Defaults**
   ```csharp
   [Column(DefaultValue = "CURRENT_TIMESTAMP")]  // Creation timestamp
   [Column(DefaultValue = "1")]                 // Boolean true
   [Column(DefaultValue = "NULL")]              // Nullable
   ```

6. **Enable Backups for Production**
   ```csharp
   // Always create backups before schema changes in production
   await mysql2.SyncTableAsync<User>(createBackup: true);
   ```

7. **Monitor Migrations**
   ```csharp
   // Regularly check for failed migrations
   var failed = await migrationTracker.GetFailedMigrationsAsync();
   if (failed.Any())
   {
       // Alert or log critical error
   }
   ```

## Troubleshooting

### Table Not Being Created

1. Verify model has `ColumnAttribute` on properties
2. Check database connection is working
3. Review logs for specific error messages
4. Ensure user has CREATE TABLE permissions

### Schema Sync Not Applying Changes

1. Check if `EnableAutoSync` is true in configuration
2. Verify model changes are correct
3. Look for migration failures in history
4. Review backup files to understand current state

### Backup Directory Issues

1. Verify `data/` directory exists and is writable
2. Check disk space availability
3. Review permissions on backup directory
4. Look for errors in logs mentioning backup operations

### Performance Issues

1. Disable backups for non-critical tables: `createBackup: false`
2. Cleanup old migrations and backups regularly
3. Index frequently queried columns
4. Use connection pooling (enabled by default)

## Migration from CL.MySQL

The new TableSync is compatible with the old CL.MySQL's `SyncTable` functionality but with improvements:

**Old API:**
```csharp
MySql_Queries.DataModel.TableSync<Models>(connectionId);
```

**New API:**
```csharp
await mysql2.SyncTableAsync<Models>(connectionId);
```

Main improvements:
- ✅ Async/await support
- ✅ Migration tracking
- ✅ Automatic backups
- ✅ Better error handling
- ✅ CodeLogic framework integration
- ✅ Batch synchronization

---

**Version:** 2.0.0
**Last Updated:** 2025-10-27
**Library:** CL.MySQL2
