# CL.PostgreSQL - Type-Safe LINQ PostgreSQL Library

A fully integrated PostgreSQL database library for the CodeLogic framework with **LINQ support** for compile-time type-safe queries, high-performance database operations, comprehensive logging, and connection management.

## ✨ Key Features

### 🔐 **Type-Safe LINQ Queries** ⭐
- **Expression Tree Support**: Compile-time checked queries with full IntelliSense
- **No Magic Strings**: Property names checked by compiler at build time
- **Refactoring Safe**: Rename properties and the compiler updates queries
- **Strong Typing**: Type mismatches caught before runtime

### 🔌 Connection Management
- **Connection Pooling**: Configurable min/max pool sizes for optimal performance
- **Multi-Database Support**: Register and manage multiple database connections simultaneously
- **Connection Caching**: Efficient caching of connection strings
- **Health Checks**: Built-in connection testing and health monitoring
- **Transaction Support**: Full ACID compliance with automatic rollback

### 📦 CRUD Operations with Repository Pattern
- **Generic Repository<T>**: Type-safe CRUD operations for any model
- **Automatic Type Mapping**: Reflection-based property-to-column mapping
- **SQL Injection Protection**: Parameterized queries for all operations
- **Query Result Caching**: Configurable in-memory caching with TTL
- **Pagination Support**: Built-in paging with page numbers and sizes

### 🚀 Type-Safe LINQ Query Builder
- **Lambda Expressions**: `Where(u => u.IsActive && u.Age > 18)`
- **Full LINQ Support**: Select, Where, OrderBy, GroupBy, Join, Aggregates
- **String Operations**: Contains, StartsWith, EndsWith with automatic LIKE translation
- **Comparison Operators**: =, !=, <, >, <=, >=, IN, BETWEEN
- **Logical Operators**: AND, OR with proper precedence
- **Aggregates**: COUNT, SUM, AVG, MIN, MAX with automatic SQL generation
- **Sorting & Pagination**: OrderBy, OrderByDescending, Take, Skip
- **SQL Debugging**: ToSql() method for query inspection

### 🔄 Automatic Schema Synchronization
- **Model-to-Database Sync**: Automatically create and update tables from C# models
- **Column Management**: Add, remove, and modify columns automatically
- **Index Management**: Create and manage indexes including composite indexes
- **Backup Management**: Automatic backups before schema changes
- **Migration Tracking**: Complete migration history logging

### 💾 Advanced Features
- **JSONB Support**: Native JSONB column support for complex data
- **Array Support**: INTEGER[], BIGINT[], TEXT[], NUMERIC[] arrays
- **UUID Support**: First-class UUID/GUID support
- **Type Conversion**: Automatic C# <-> PostgreSQL type conversion
- **Timestamp Management**: Automatic CURRENT_TIMESTAMP and update tracking
- **Foreign Keys**: Built-in foreign key constraint support

### 🔐 Security & Performance
- **Parameterized Queries**: All SQL queries use parameters to prevent injection
- **Connection Pooling**: Reuse connections efficiently
- **Query Caching**: Reduce database load with result caching
- **Slow Query Logging**: Optional logging of queries exceeding threshold
- **Detailed Logging**: Comprehensive operation logging for debugging

## Installation

Add project reference to your CodeLogic project:

```xml
<ItemGroup>
  <ProjectReference Include="...\CL.PostgreSQL\CL.PostgreSQL.csproj" />
</ItemGroup>
```

## Configuration

Create a `config/postgresql.json` file in your application root:

```json
{
  "Default": {
    "enabled": true,
    "host": "localhost",
    "port": 5432,
    "database": "your_database",
    "username": "postgres",
    "password": "your_password",
    "min_pool_size": 5,
    "max_pool_size": 100,
    "max_idle_time": 60,
    "connection_timeout": 30,
    "command_timeout": 30,
    "ssl_mode": "Prefer",
    "enable_logging": true,
    "enable_caching": true,
    "default_cache_ttl": 300,
    "enable_auto_sync": true,
    "log_slow_queries": true,
    "slow_query_threshold": 1000
  }
}
```

## Quick Start

### 1. Define Your Models

```csharp
using CL.PostgreSQL.Models;

[Table(Schema = "public")]
public class User
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string? Username { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true, Unique = true)]
    public string? Email { get; set; }

    [Column(DataType = DataType.Timestamp, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(DataType = DataType.Bool, DefaultValue = "true")]
    public bool IsActive { get; set; }
}
```

### 2. Use the Repository Pattern for CRUD

```csharp
// Get the library
var library = context.GetLibrary<PostgreSQL2Library>("cl.postgresql");

// Create a repository
var userRepo = library.GetRepository<User>();

// CREATE - Insert
var user = new User { Username = "john_doe", Email = "john@example.com" };
var result = await userRepo.InsertAsync(user);

// READ - Get by ID
var fetchedUser = await userRepo.GetByIdAsync(1);

// READ - Get all
var allUsers = await userRepo.GetAllAsync();

// READ - Paginated
var pagedUsers = await userRepo.GetPagedAsync(page: 1, pageSize: 10);

// UPDATE
user.IsActive = false;
await userRepo.UpdateAsync(user);

// DELETE
await userRepo.DeleteAsync(user.Id);

// COUNT
var totalUsers = await userRepo.CountAsync();
```

### 3. Use Type-Safe LINQ Queries ⭐

```csharp
var queryBuilder = library.GetQueryBuilder<User>();

// ✅ Simple LINQ query (type-safe!)
var activeUsers = await queryBuilder
    .Where(u => u.IsActive == true)
    .OrderByDescending(u => u.CreatedAt)
    .ExecuteAsync();

// ✅ Multiple conditions with AND
var results = await queryBuilder
    .Where(u => u.IsActive && u.CreatedAt > startDate)
    .OrderBy(u => u.Username)
    .Take(10)
    .ExecuteAsync();

// ✅ String operations
var emailUsers = await queryBuilder
    .Where(u => u.Email.Contains("@company.com"))
    .ExecuteAsync();

// ✅ Collection filtering
var userIds = new[] { 1, 2, 3 };
var filtered = await queryBuilder
    .Where(u => userIds.Contains((int)u.Id))
    .ExecuteAsync();

// ✅ Paged results
var paged = await queryBuilder
    .Where(u => u.IsActive)
    .ToPagedAsync(page: 1, pageSize: 20);

// ✅ Count with filtering
var count = await queryBuilder
    .Where(u => u.IsActive)
    .CountAsync();

// ✅ Get first
var firstUser = await queryBuilder
    .Where(u => u.Username == "john_doe")
    .FirstOrDefaultAsync();
```

## LINQ Patterns

### Comparisons
```csharp
.Where(u => u.Age > 18)           // Greater than
.Where(u => u.Age >= 18)          // Greater or equal
.Where(u => u.Age < 65)           // Less than
.Where(u => u.CreatedAt >= date)  // Date comparison
```

### Logical Operators
```csharp
.Where(u => u.IsActive && u.Age > 18)                  // AND
.Where(u => u.Role == "Admin" || u.Role == "Moderator")  // OR
.Where(u => u.IsActive && (u.Age > 18 || u.VIP))       // Complex
```

### String Methods
```csharp
.Where(u => u.Email.Contains("@gmail.com"))    // Contains (LIKE)
.Where(u => u.Username.StartsWith("admin_"))   // Starts with
.Where(u => u.Domain.EndsWith(".com"))         // Ends with
```

### Collections
```csharp
.Where(u => new[] { 1, 2, 3 }.Contains((int)u.Id))  // IN clause
.Where(u => myList.Contains(u.Status))               // From variable
```

### Ordering
```csharp
.OrderBy(u => u.CreatedAt)              // Ascending
.OrderByDescending(u => u.Score)        // Descending
.ThenBy(u => u.Email)                   // Then by
.ThenByDescending(u => u.Id)            // Then by descending
```

### Aggregates
```csharp
.Count()                             // COUNT(*)
.Sum(u => u.TotalSpent)             // SUM
.Avg(u => u.Rating)                 // AVG
.Min(u => u.Age)                    // MIN
.Max(u => u.Score)                  // MAX
```

### Pagination
```csharp
.Take(10)                    // LIMIT
.Skip(20)                    // OFFSET
.ToPagedAsync(1, 20)        // Get page with metadata
```

## Synchronize Tables

```csharp
// Sync single table
await library.SyncTableAsync<User>(createBackup: true);

// Sync multiple tables
var types = new[] { typeof(User), typeof(Post) };
var results = await library.SyncTablesAsync(types, createBackup: true);

// Sync entire namespace
await library.SyncNamespaceAsync(
    "MyApp.Models",
    createBackup: true,
    includeDerivedNamespaces: true
);
```

## Model Attributes

### [TableAttribute]
```csharp
[Table(
    Name = "custom_table_name",   // Optional
    Schema = "public",             // Optional, defaults to "public"
    Comment = "Table description"  // Optional
)]
public class MyModel { }
```

### [ColumnAttribute]
```csharp
[Column(
    DataType = DataType.VarChar,   // Required
    Name = "custom_column",        // Optional
    Size = 255,                    // For VARCHAR/CHAR
    Primary = false,               // Primary key
    AutoIncrement = false,         // Auto-increment
    NotNull = false,               // NOT NULL
    Unique = false,                // UNIQUE
    Index = false,                 // Create index
    DefaultValue = null,           // Default value
    OnUpdateCurrentTimestamp = false  // Auto-update
)]
public string? MyColumn { get; set; }
```

### [ForeignKeyAttribute]
```csharp
[ForeignKey(
    ReferenceTable = "users",
    ReferenceColumn = "id",
    OnDelete = ForeignKeyAction.Cascade
)]
public long UserId { get; set; }
```

### [IgnoreAttribute]
```csharp
[Ignore]  // Skip in database operations
public string ComputedValue { get; set; }
```

### [CompositeIndexAttribute]
```csharp
[CompositeIndex("idx_user_email", "UserId", "Email", Unique = true)]
public class UserEmail { }
```

## Data Types

Comprehensive PostgreSQL data type support:

- **Integers**: SmallInt, Int, BigInt
- **Decimals**: Real, DoublePrecision, Numeric
- **Dates/Times**: Timestamp, TimestampTz, Date, Time, TimeTz
- **Text**: Char, VarChar, Text
- **JSON**: Json, Jsonb
- **Special**: Uuid, Bool, Bytea
- **Arrays**: IntArray, BigIntArray, TextArray, NumericArray

## Best Practices

1. **Always use LINQ expressions** instead of magic strings - compiler catches errors
2. **Cache frequently accessed data** by setting cacheTtl parameter
3. **Use pagination** for large result sets
4. **Index columns** that are frequently used in WHERE clauses
5. **Create backups** before running SyncTableAsync in production
6. **Use transactions** for multi-step operations
7. **Enable logging** during development to debug queries
8. **Use JSONB** for flexible schema columns
9. **Leverage array types** for collections stored in single columns
10. **Define foreign keys** for referential integrity

## Performance Tips

- **Connection Pooling**: Adjust min/max pool sizes based on load
- **Query Caching**: Enable and tune cache TTL for read-heavy operations
- **Indexes**: Create indexes on frequently queried columns
- **Pagination**: Use pagination for large result sets
- **Async/Await**: Always use async methods for non-blocking I/O
- **LINQ Efficiency**: Complex LINQ expressions translate to optimal SQL

## Troubleshooting

### Connection Issues
Verify connection string and ensure PostgreSQL is running:
```json
{
  "host": "localhost",
  "port": 5432,
  "database": "test_db",
  "username": "postgres"
}
```

### Type Safety Errors
Ensure column names in LINQ match property names:
```csharp
// ✓ Correct - property name matches column
public string Username { get; set; }
.Where(u => u.Username == "john")

// ✗ Wrong - property/column mismatch
.Where(u => u.UserNam == "john")  // Compile error!
```

### Performance Issues
Enable logging to see slow queries:
```json
{
  "enable_logging": true,
  "log_slow_queries": true,
  "slow_query_threshold": 1000
}
```

## Supported PostgreSQL Versions

- PostgreSQL 12.x and above

## Dependencies

- **Npgsql**: PostgreSQL data provider for .NET
- **CodeLogic**: Framework integration
- **.NET 10.0**: Target framework

## Version

**Current Version**: 2.0.0 with LINQ Support

## License

This library is part of the CodeLogic framework and follows the same licensing.

## Support

For issues, questions, or feature requests, please refer to the CodeLogic documentation or create an issue in the repository.

---

**Happy querying with type-safe LINQ!** 🚀
