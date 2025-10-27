# TableSync - Namespace Synchronization Guide

## Overview

Namespace synchronization allows you to sync all model classes in a namespace automatically. This is particularly useful when you have all your database models organized in a dedicated namespace (e.g., `App.DatabaseModels`, `MyProject.Models`, etc.).

## Quick Start

### Sync All Models in a Namespace

```csharp
var mysql2 = serviceProvider.GetRequiredService<MySQL2Library>();

// Sync all models in the "App.DatabaseModels" namespace
var results = await mysql2.SyncNamespaceAsync(
    namespaceName: "App.DatabaseModels",
    connectionId: "Default",
    createBackup: true,
    includeDerivedNamespaces: false
);

// Check results
foreach (var (table, success) in results)
{
    Console.WriteLine($"{table}: {(success ? "✓ Success" : "✗ Failed")}");
}
```

## Parameters

### `namespaceName` (string, required)
The full namespace name to scan for models.

**Examples:**
```csharp
"App.DatabaseModels"
"MyProject.Data.Models"
"Company.Product.Database.Entities"
```

### `connectionId` (string, optional)
The database connection to use for synchronization.

**Default:** `"Default"`

**Example:**
```csharp
await mysql2.SyncNamespaceAsync(
    "App.DatabaseModels",
    connectionId: "Analytics"  // Use specific connection
);
```

### `createBackup` (bool, optional)
Whether to create schema backups before making changes.

**Default:** `true`

**Example:**
```csharp
await mysql2.SyncNamespaceAsync(
    "App.DatabaseModels",
    createBackup: false  // Skip backups for non-production
);
```

### `includeDerivedNamespaces` (bool, optional)
Whether to include namespaces that start with the given namespace (sub-namespaces).

**Default:** `false`

**Example:**
```csharp
// Sync only App.DatabaseModels namespace
await mysql2.SyncNamespaceAsync(
    "App.DatabaseModels",
    includeDerivedNamespaces: false
);

// Sync App.DatabaseModels AND App.DatabaseModels.* namespaces
await mysql2.SyncNamespaceAsync(
    "App.DatabaseModels",
    includeDerivedNamespaces: true
);
```

## How It Works

1. **Discovery** - Scans all loaded assemblies for types in the specified namespace
2. **Filtering** - Filters to only include classes with `[Table]` attribute
3. **Validation** - Excludes abstract classes, interfaces, and generic types
4. **Synchronization** - Syncs all found models using batch synchronization
5. **Logging** - Records results for each table

## Use Cases

### Case 1: Simple Models Namespace

Directory structure:
```
App/
├── Models/
│   ├── User.cs
│   ├── Order.cs
│   └── Product.cs
└── Services/
```

Namespace: `App.Models`

```csharp
// Sync all 3 models at once
var results = await mysql2.SyncNamespaceAsync("App.Models");
```

### Case 2: Organized Multi-Level Namespaces

Directory structure:
```
MyProject/
├── Database/
│   ├── Models/
│   │   ├── UserModel.cs
│   │   ├── OrderModel.cs
│   │   └── ProductModel.cs
│   └── Views/
│       ├── UserView.cs
│       └── ReportView.cs
```

Namespace: `MyProject.Database.Models`

```csharp
// Sync only models (not views)
var results = await mysql2.SyncNamespaceAsync("MyProject.Database.Models");
```

### Case 3: Syncing All Database Objects

Directory structure:
```
Company/
├── App/
│   ├── Database/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Order.cs
│   │   │   └── Product.cs
│   │   ├── Views/
│   │   │   ├── UserStats.cs
│   │   │   └── OrderAnalysis.cs
```

Namespace: `Company.App.Database`

```csharp
// Sync everything under Database (Entities AND Views)
var results = await mysql2.SyncNamespaceAsync(
    "Company.App.Database",
    includeDerivedNamespaces: true  // Includes sub-namespaces
);
```

## Return Value

Returns a `Dictionary<string, bool>` where:
- **Key:** Table name (string)
- **Value:** Success status (bool) - `true` if synced successfully, `false` if failed

**Example:**
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");

// results = {
//   "users": true,
//   "orders": true,
//   "products": false  // Failed due to some error
// }
```

## Filtering Logic

The namespace sync feature automatically filters types based on:

✅ **Included:**
- Classes (not abstract)
- With `[Table]` attribute
- Public classes
- Non-generic types

❌ **Excluded:**
- Abstract classes
- Interfaces
- Generic type definitions
- Classes without `[Table]` attribute
- Classes in different namespaces

## Logging

All namespace sync operations are logged with detailed information:

```
[INFO] 2025-10-27 14:30:45 - Starting namespace synchronization for 'App.DatabaseModels' (includeDerived: false)
[INFO] 2025-10-27 14:30:45 - Found 5 model(s) in namespace 'App.DatabaseModels'. Starting synchronization...
[INFO] 2025-10-27 14:30:45 - Starting batch table synchronization for 5 model(s)
[INFO] 2025-10-27 14:30:46 - Starting table synchronization for model 'User' (table: 'users')
[INFO] 2025-10-27 14:30:46 - Successfully created table 'users'
[INFO] 2025-10-27 14:30:46 - Starting table synchronization for model 'Order' (table: 'orders')
[INFO] 2025-10-27 14:30:47 - Existing table 'orders' synced successfully
[INFO] 2025-10-27 14:30:47 - Batch synchronization completed. Success: 5/5
[DEBUG] 2025-10-27 14:30:47 - Could not load types from assembly 'System.Net.Http': (no error)
```

## Error Handling

Namespace sync continues even if individual tables fail:

```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");

// Check for failures
var failed = results.Where(r => !r.Value).ToList();
if (failed.Any())
{
    Console.WriteLine($"⚠ {failed.Count} table(s) failed to sync:");
    foreach (var (table, _) in failed)
    {
        Console.WriteLine($"  - {table}");
    }
}
```

## Startup Integration

Use namespace sync during application startup:

```csharp
// In Startup.cs or Program.cs
var mysql2 = serviceProvider.GetRequiredService<MySQL2Library>();

var syncResults = await mysql2.SyncNamespaceAsync(
    "MyApp.Database.Models",
    createBackup: !isDevelopment
);

var successCount = syncResults.Count(r => r.Value);
Console.WriteLine($"✓ Synced {successCount}/{syncResults.Count} tables");

if (syncResults.Values.Contains(false))
{
    logger.LogWarning("Some tables failed to sync");
}
```

## Best Practices

### 1. Use Dedicated Namespaces
Create a dedicated namespace for your models:
```csharp
namespace MyApp.Database.Models  // ✓ Good
{
    [Table(Name = "users")]
    public class User { }
}

// NOT:
namespace MyApp.Services  // ✗ Avoid mixing with other code
{
    [Table(Name = "users")]
    public class User { }
}
```

### 2. Consistent Naming
Use consistent patterns across your models:
```csharp
[Table(Name = "users")]
public class User { }

[Table(Name = "orders")]
public class Order { }

[Table(Name = "products")]
public class Product { }
```

### 3. Handle Failures
Always check results and handle failures:
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");

var failed = results.Where(r => !r.Value).Select(r => r.Key).ToList();
if (failed.Any())
{
    throw new InvalidOperationException(
        $"Failed to sync tables: {string.Join(", ", failed)}"
    );
}
```

### 4. Disable for Development When Appropriate
```csharp
if (environment.IsProduction())
{
    // Create backups in production
    await mysql2.SyncNamespaceAsync("App.Models", createBackup: true);
}
else
{
    // Faster sync in development
    await mysql2.SyncNamespaceAsync("App.Models", createBackup: false);
}
```

### 5. Log Sync Results
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");

foreach (var (table, success) in results)
{
    if (success)
        logger.LogInformation($"✓ Synced table: {table}");
    else
        logger.LogError($"✗ Failed to sync table: {table}");
}
```

## Troubleshooting

### No Models Found
```
[WARN] 2025-10-27 14:30:45 - No model classes with [Table] attribute found in namespace 'App.Models'
```

**Solution:**
- Verify namespace name is correct
- Ensure models have `[Table]` attribute
- Check that assemblies are loaded

### Models in Sub-namespaces Not Synced
```csharp
// If you have:
// App.Models.Users.User
// App.Models.Orders.Order

// This won't find them:
await mysql2.SyncNamespaceAsync("App.Models", includeDerivedNamespaces: false);

// Do this instead:
await mysql2.SyncNamespaceAsync("App.Models", includeDerivedNamespaces: true);
```

### Assembly Load Errors
```
[DEBUG] 2025-10-27 14:30:45 - Could not load types from assembly 'SomeAssembly': ...
```

**Cause:** Some assemblies may not be loadable. This is normal and non-critical. The sync continues with other assemblies.

## API Summary

```csharp
// Sync a namespace
Task<Dictionary<string, bool>> SyncNamespaceAsync(
    string namespaceName,
    string connectionId = "Default",
    bool createBackup = true,
    bool includeDerivedNamespaces = false
);
```

## Examples

### Example 1: Simple Sync
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");
Console.WriteLine($"Synced {results.Count} tables");
```

### Example 2: With Error Handling
```csharp
var results = await mysql2.SyncNamespaceAsync("App.Models");

foreach (var (table, success) in results)
{
    var status = success ? "✓" : "✗";
    Console.WriteLine($"{status} {table}");
}
```

### Example 3: Conditional Backup
```csharp
var isDev = environment.IsDevelopment();
var results = await mysql2.SyncNamespaceAsync(
    "App.Models",
    createBackup: !isDev  // No backups in dev for speed
);
```

### Example 4: Multiple Namespaces
```csharp
var modelNamespaces = new[]
{
    "App.Database.Models",
    "App.Database.Views",
    "App.Database.MaterializedViews"
};

foreach (var ns in modelNamespaces)
{
    var results = await mysql2.SyncNamespaceAsync(ns);
    Console.WriteLine($"Synced {ns}: {results.Count(r => r.Value)}/{results.Count}");
}
```

## See Also

- `SyncTableAsync<T>()` - Sync single table
- `SyncTablesAsync(types[])` - Sync multiple specific tables
- Migration Tracking - Track all schema changes
- Backup Management - Manage SQL backups

---

**Version:** 2.0.0
**Feature Added:** 2025-10-27
**Library:** CL.MySQL2
