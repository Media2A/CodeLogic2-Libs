# Namespace Sync - Quick Reference Card

## One-Liner Usage

```csharp
// Sync all models in "App.Models" namespace
await mysql2.SyncNamespaceAsync("App.Models");
```

## Full Method Signature

```csharp
Task<Dictionary<string, bool>> SyncNamespaceAsync(
    string namespaceName,
    string connectionId = "Default",
    bool createBackup = true,
    bool includeDerivedNamespaces = false
);
```

## Common Patterns

### Pattern 1: Basic Sync
```csharp
var results = await mysql2.SyncNamespaceAsync("App.DatabaseModels");
```

### Pattern 2: With Sub-namespaces
```csharp
var results = await mysql2.SyncNamespaceAsync(
    "App.Database",
    includeDerivedNamespaces: true  // App.Database.*
);
```

### Pattern 3: Error Handling
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");
var failed = results.Where(r => !r.Value).ToList();

if (failed.Any())
    throw new InvalidOperationException(
        $"Failed: {string.Join(", ", failed.Keys)}"
    );
```

### Pattern 4: Startup Integration
```csharp
var syncResults = await mysql2.SyncNamespaceAsync(
    "MyApp.Models",
    createBackup: !isDevelopment
);
```

### Pattern 5: Sync Multiple Namespaces
```csharp
var namespaces = new[] { "App.Models", "App.Views", "App.Reports" };
foreach (var ns in namespaces)
{
    var results = await mysql2.SyncNamespaceAsync(ns);
    var success = results.Count(r => r.Value);
    Console.WriteLine($"{ns}: {success}/{results.Count}");
}
```

## Key Parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `namespaceName` | string | required | Full namespace (e.g., "App.Models") |
| `connectionId` | string | "Default" | Database connection ID |
| `createBackup` | bool | true | Create schema backups |
| `includeDerivedNamespaces` | bool | false | Include sub-namespaces |

## What Gets Synced

✓ **Synced:**
- Classes with `[Table]` attribute
- Non-abstract classes
- Public classes
- Non-generic types

✗ **NOT Synced:**
- Classes without `[Table]` attribute
- Abstract classes
- Interfaces
- Generic types

## Return Value

`Dictionary<string, bool>` where:
- **Key** = Table name
- **Value** = Success (true/false)

Example:
```csharp
{
  "users": true,
  "orders": true,
  "products": false  // Failed
}
```

## Logging Output

```
[INFO] Found 5 model(s) in namespace 'App.Models'
[INFO] Starting batch table synchronization for 5 model(s)
[INFO] Starting table synchronization for model 'User' (table: 'users')
[INFO] Successfully created table 'users'
...
[INFO] Batch synchronization completed. Success: 5/5
```

## Advanced Usage

### Check Individual Failures
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");
foreach (var (table, success) in results)
{
    if (!success)
        logger.LogError($"Failed to sync {table}");
}
```

### Get Sync Service for Advanced Operations
```csharp
var syncService = mysql2.GetTableSyncService();
var migrations = syncService.GetMigrationTracker();
var backups = syncService.GetBackupManager();
```

### Access Migration History
```csharp
var tracker = syncService.GetMigrationTracker();
var history = await tracker.GetMigrationHistoryAsync();
foreach (var migration in history)
{
    Console.WriteLine($"{migration.AppliedAt} - {migration.MigrationType}");
}
```

## Real-World Examples

### Example: Multi-Database Sync
```csharp
var connections = new[] { "Default", "Analytics", "Reporting" };
foreach (var conn in connections)
{
    await mysql2.SyncNamespaceAsync(
        "MyApp.Models",
        connectionId: conn
    );
}
```

### Example: Conditional Backup
```csharp
var results = await mysql2.SyncNamespaceAsync(
    "App.Models",
    createBackup: environment.IsProduction()
);
```

### Example: With Retry Logic
```csharp
int attempts = 0;
Dictionary<string, bool> results = new();

while (attempts < 3)
{
    results = await mysql2.SyncNamespaceAsync("App.Models");

    var failureCount = results.Count(r => !r.Value);
    if (failureCount == 0) break;

    attempts++;
    if (attempts < 3)
        await Task.Delay(1000);  // Wait before retry
}
```

## Common Namespaces

```csharp
"App.Models"
"App.Database.Models"
"MyProject.Entities"
"Company.Product.Data"
"WebApp.Core.Models"
```

## Troubleshooting Quick Tips

| Issue | Solution |
|-------|----------|
| No models found | Check namespace name and `[Table]` attributes |
| Some sync failures | Check logs and use error handling pattern |
| Sub-namespaces not synced | Set `includeDerivedNamespaces: true` |
| Backups not created | Check `data/backups/` directory exists |
| Slow performance | Disable backups with `createBackup: false` |

## Key Differences from Other Methods

```csharp
// Single table - must specify type
await mysql2.SyncTableAsync<User>();

// Multiple tables - must list types
await mysql2.SyncTablesAsync(
    new Type[] { typeof(User), typeof(Order) }
);

// Entire namespace - AUTO-discovery! (NEW)
await mysql2.SyncNamespaceAsync("App.Models");
```

## DocumentationLinks

- Full Guide: `TABLESYNC_NAMESPACE_SYNC.md`
- Quick Start: `TABLESYNC_QUICKSTART.md`
- Complete Guide: `TABLESYNC_GUIDE.md`
- Implementation: `TABLESYNC_IMPLEMENTATION_SUMMARY.md`

---

**Version:** 2.0.0
**Feature:** Namespace Synchronization
**Library:** CL.MySQL2
