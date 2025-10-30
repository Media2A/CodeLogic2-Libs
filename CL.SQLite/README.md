# CL.SQLite - SQLite Library for CodeLogic Framework

A fully integrated SQLite database library for the CodeLogic framework, featuring **type-safe LINQ query support**, model-based ORM, connection pooling, automatic schema synchronization, and comprehensive database operations.

## Features

- **Type-Safe LINQ Queries** ⭐ - Compile-time error checking with lambda expressions
- **Full CodeLogic Integration**: Seamlessly integrates with the framework's logging and dependency injection
- **Connection Management**: Optimized connection pooling with automatic lifecycle management
- **Repository Pattern**: Generic repository implementation for type-safe CRUD operations
- **Schema Synchronization**: Automatic table creation and schema updates from C# models
- **Model-Based ORM**: Attribute-based mapping from C# classes to database tables
- **Type Safety**: Strong typing with attribute-based model mapping
- **Comprehensive Logging**: Integrated with CodeLogic's logging system

## Installation

Add the project reference to your application:

```xml
<ProjectReference Include="path/to/CL.SQLite/CL.SQLite.csproj" />
```

## Configuration

Create a `sqlite.json` file in your `config` directory:

```json
{
  "Default": {
    "database_path": "database.db",
    "connection_timeout": 30,
    "command_timeout": 120,
    "max_pool_size": 10,
    "enable_foreign_keys": true,
    "use_wal": true,
    "cache_mode": "default",
    "skip_table_sync": false
  }
}
```

## Usage

### Defining Models

```csharp
using CL.SQLite.Models;

[SQLiteTable(TableName = "users")]
public class User
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(ColumnName = "username")]
    public string Username { get; set; }

    [SQLiteColumn(ColumnName = "email")]
    public string Email { get; set; }

    [SQLiteColumn(ColumnName = "is_active", DefaultValue = "1")]
    public bool IsActive { get; set; }

    [SQLiteColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### Accessing the Library

```csharp
// Get the SQLite library instance
var sqlite = libraryManager.GetLibrary<SQLiteLibrary>("cl.sqlite");

// Create a repository for CRUD operations
var userRepository = sqlite.CreateRepository<User>();

// Create a type-safe LINQ query builder
var queryBuilder = sqlite.CreateQueryBuilder<User>();
```

### CRUD Operations with Repository

```csharp
// Insert
var newUser = new User
{
    Username = "john_doe",
    Email = "john@example.com",
    IsActive = true,
    CreatedAt = DateTime.Now
};
var insertResult = await userRepository.InsertAsync(newUser);

if (insertResult.IsSuccess)
{
    Console.WriteLine($"User created with ID: {insertResult.Data}");
}

// Get by ID
var getResult = await userRepository.GetByIdAsync(1);
if (getResult.IsSuccess && getResult.Data != null)
{
    var user = getResult.Data;
    Console.WriteLine($"Found user: {user.Username}");
}

// Get all
var allUsersResult = await userRepository.GetAllAsync(limit: 100);
if (allUsersResult.IsSuccess)
{
    foreach (var user in allUsersResult.Data)
    {
        Console.WriteLine($"User: {user.Username}");
    }
}

// Update
user.Email = "newemail@example.com";
var updateResult = await userRepository.UpdateAsync(user);

// Delete
var deleteResult = await userRepository.DeleteAsync(1);
```

### Type-Safe LINQ Queries ⭐

The QueryBuilder now supports type-safe LINQ expressions for compile-time error checking!

#### Simple Queries

```csharp
// Simple WHERE clause (type-safe!)
var activeUsers = await queryBuilder
    .Where(u => u.IsActive == true)
    .ExecuteAsync();

// Multiple conditions with AND
var results = await queryBuilder
    .Where(u => u.IsActive && u.CreatedAt > DateTime.Now.AddMonths(-1))
    .ExecuteAsync();

// OR conditions
var vipUsers = await queryBuilder
    .Where(u => u.Email.EndsWith("@vip.com") || u.Email.EndsWith("@premium.com"))
    .ExecuteAsync();
```

#### Sorting & Pagination

```csharp
// Simple ordering
var sortedUsers = await queryBuilder
    .Where(u => u.IsActive)
    .OrderBy(u => u.Username)
    .ExecuteAsync();

// Descending order
var newestUsers = await queryBuilder
    .OrderByDescending(u => u.CreatedAt)
    .ExecuteAsync();

// Multiple sort criteria
var sorted = await queryBuilder
    .OrderBy(u => u.IsActive)
    .ThenByDescending(u => u.CreatedAt)
    .ExecuteAsync();

// Limit results
var topTen = await queryBuilder
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.CreatedAt)
    .Take(10)
    .ExecuteAsync();

// Pagination
var page = await queryBuilder
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.CreatedAt)
    .ToPagedAsync(page: 1, pageSize: 20);

Console.WriteLine($"Total: {page.Data.TotalItems}");
Console.WriteLine($"Pages: {page.Data.TotalPages}");
```

#### String Operations

```csharp
// Contains (LIKE with %)
var matchingUsers = await queryBuilder
    .Where(u => u.Email.Contains("@company.com"))
    .ExecuteAsync();

// StartsWith (LIKE prefix)
var adminUsers = await queryBuilder
    .Where(u => u.Username.StartsWith("admin_"))
    .ExecuteAsync();

// EndsWith (LIKE suffix)
var gmailUsers = await queryBuilder
    .Where(u => u.Email.EndsWith("@gmail.com"))
    .ExecuteAsync();

// Combined string operations
var filtered = await queryBuilder
    .Where(u => u.Username.Contains("John") && u.Email.EndsWith("@example.com"))
    .ExecuteAsync();
```

#### Collection Filtering (IN clause)

```csharp
// Filter by ID list
var userIds = new[] { 1, 2, 3, 4, 5 };
var specificUsers = await queryBuilder
    .Where(u => userIds.Contains(u.Id))
    .ExecuteAsync();

// Filter by status list
var statuses = new[] { "Active", "Premium", "Trial" };
var statusUsers = await queryBuilder
    .Where(u => statuses.Contains(u.Email))
    .ExecuteAsync();
```

#### Aggregates & Statistics

```csharp
// Count
var totalActive = await queryBuilder
    .Where(u => u.IsActive)
    .CountAsync();

// Sum (with numeric columns)
var stats = await queryBuilder
    .Sum(u => u.Id, "total_ids")
    .ExecuteAsync();

// Average
var avgStats = await queryBuilder
    .Avg(u => u.Id, "average_id")
    .ExecuteAsync();

// Min/Max
var minMax = await queryBuilder
    .Min(u => u.Id, "min_id")
    .Max(u => u.Id, "max_id")
    .ExecuteAsync();
```

#### First or Default

```csharp
var firstUser = await queryBuilder
    .Where(u => u.Email == "john@example.com")
    .FirstOrDefaultAsync();

if (firstUser.IsSuccess && firstUser.Data != null)
{
    Console.WriteLine($"Found: {firstUser.Data.Username}");
}
```

#### View Generated SQL

```csharp
var sql = queryBuilder
    .Where(u => u.IsActive && u.Email.Contains("@company.com"))
    .OrderByDescending(u => u.CreatedAt)
    .Take(10)
    .ToSql();

Console.WriteLine(sql);
// Output: SELECT * FROM users WHERE is_active = @p0 AND email LIKE @p1 ORDER BY created_at DESC LIMIT 10
```

## LINQ vs Magic Strings: Why LINQ is Better

### Before (Magic Strings - ❌ Not Type-Safe)

```csharp
// ❌ Typo won't be caught until runtime!
// No built-in support for magic strings - use Repository instead
```

### After (LINQ - ✅ Type-Safe!)

```csharp
// ✅ Compiler catches errors immediately!
var users = await queryBuilder
    .Where(u => u.IsActive == true)     // ✓ Compile-time check
    .Where(u => u.CreatedAt > startDate)// ✓ Type-safe comparison
    .OrderByDescending(u => u.CreatedAt) // ✓ IntelliSense & compiler
    .ExecuteAsync();
```

**Benefits of LINQ:**
- ✅ Compile-time error detection
- ✅ Full IntelliSense support in your IDE
- ✅ Safe refactoring (rename properties and queries update automatically)
- ✅ Type-safe comparisons
- ✅ Consistent with MySQL2 and PostgreSQL libraries

## Schema Synchronization

Automatically synchronize your database schema with your C# models:

```csharp
var tableSync = sqlite.TableSyncService;

// Sync single table
await tableSync.SyncTableAsync<User>();

// Sync multiple tables
var types = new[] { typeof(User), typeof(Product) };
var results = await tableSync.SyncTablesAsync(types);

// Sync entire namespace
await tableSync.SyncNamespaceAsync("MyApp.Models", includeDerivedNamespaces: true);
```

## Attributes

### [SQLiteTableAttribute]
```csharp
[SQLiteTable(TableName = "custom_table_name")]
public class MyModel { }
```

### [SQLiteColumnAttribute]
```csharp
[SQLiteColumn(
    ColumnName = "custom_column",
    IsPrimaryKey = false,
    IsAutoIncrement = false,
    IsNotNull = false,
    IsUnique = false,
    IsIndexed = false,
    DefaultValue = null
)]
public string MyColumn { get; set; }
```

### [SQLiteForeignKeyAttribute]
```csharp
[SQLiteForeignKey(
    ReferenceTable = "users",
    ReferenceColumn = "id",
    OnDelete = ForeignKeyAction.Cascade
)]
public int UserId { get; set; }
```

## Architecture

### Key Components

1. **ExpressionVisitor**: Converts LINQ expression trees to SQL WHERE conditions ⭐
2. **ConnectionManager**: Manages database connections with pooling and caching
3. **Repository<T>**: Generic repository for CRUD operations
4. **QueryBuilder<T>**: Type-safe fluent API using LINQ expressions
5. **TableSyncService**: Automatic schema synchronization
6. **SchemaAnalyzer**: Schema comparison and migration
7. **MigrationTracker**: Migration history tracking

## Dependencies

- **Microsoft.Data.Sqlite**: SQLite data provider
- **Newtonsoft.Json**: JSON configuration parsing
- **CodeLogic**: Framework integration

## Version History

### 2.0.0 (Current)
- **Type-Safe LINQ Only** ⭐ - Full LINQ expression support
- Complete rewrite for CodeLogic 2.0 framework
- Model-based ORM with attribute mapping
- Automatic schema synchronization
- Connection pooling and management
- Migration tracking
- Consistent with MySQL2 and PostgreSQL libraries

## License

Part of the CodeLogic framework by Media2A.
