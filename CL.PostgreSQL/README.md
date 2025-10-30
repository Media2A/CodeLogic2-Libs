# CL.PostgreSQL - CodeLogic PostgreSQL Library

A fully integrated PostgreSQL database library for the CodeLogic framework, providing high-performance database operations with comprehensive logging, configuration, and connection management.

## Features

### 🔌 Connection Management
- **Connection Pooling**: Configurable min/max pool sizes for optimal performance
- **Multi-Database Support**: Register and manage multiple database connections simultaneously
- **Automatic Retry**: Built-in retry logic for connection failures
- **Connection Caching**: Efficient caching of connection strings for repeated operations
- **Health Checks**: Built-in connection testing and health monitoring

### 📦 CRUD Operations with Repository Pattern
- **Generic Repository<T>**: Type-safe CRUD operations for any model
- **Automatic Type Mapping**: Reflection-based property-to-column mapping
- **SQL Injection Protection**: Parameterized queries for all operations
- **Query Result Caching**: Configurable in-memory caching with TTL
- **Pagination Support**: Built-in paging with page numbers and sizes

### 🔨 Fluent Query Builder
- **Chainable API**: Intuitive fluent interface for building complex queries
- **Flexible WHERE Clauses**: Support for =, !=, <, >, <=, >=, IN, LIKE, BETWEEN
- **JOIN Support**: INNER, LEFT, RIGHT, and FULL OUTER joins
- **Aggregates**: COUNT, SUM, AVG, MIN, MAX with grouping
- **Sorting & Pagination**: ORDER BY with ASC/DESC, LIMIT, OFFSET
- **SQL Generation**: ToSql() method for query debugging

### 🔄 Automatic Schema Synchronization
- **Model-to-Database Sync**: Automatically create and update tables from C# models
- **Column Management**: Add, remove, and modify columns automatically
- **Index Management**: Create and manage indexes including composite indexes
- **Backup Management**: Automatic backups before schema changes
- **Migration Tracking**: Complete migration history logging
- **Namespace Sync**: Synchronize all tables in a namespace at once

### 💾 Advanced Features
- **JSONB Support**: Native JSONB column support for complex data
- **Array Support**: INTEGER[], BIGINT[], TEXT[], NUMERIC[] arrays
- **UUID Support**: First-class UUID/GUID support
- **Type Conversion**: Automatic C# <-> PostgreSQL type conversion
- **Timestamp Management**: Automatic CURRENT_TIMESTAMP and update tracking
- **Foreign Keys**: Built-in foreign key constraint support
- **Unique Constraints**: Column uniqueness enforcement

### 📊 Data Types

Comprehensive PostgreSQL data type support:
- **Integers**: SmallInt, Int, BigInt
- **Decimals**: Real, DoublePrecision, Numeric
- **Dates/Times**: Timestamp, TimestampTz, Date, Time, TimeTz
- **Text**: Char, VarChar, Text
- **Binary**: Bytea
- **JSON**: Json, Jsonb
- **Special**: Uuid, Bool
- **Arrays**: IntArray, BigIntArray, TextArray, NumericArray

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
  },
  "Demo": {
    "enabled": true,
    "host": "localhost",
    "port": 5432,
    "database": "demo_database",
    "username": "postgres",
    "password": "",
    "min_pool_size": 5,
    "max_pool_size": 100,
    "ssl_mode": "Disable"
  }
}
```

## Quick Start

### 1. Define Your Models

```csharp
using CL.PostgreSQL.Models;

[Table(Schema = "public", Comment = "Users table")]
public class User
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true, Index = true)]
    public string? Username { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true, Unique = true)]
    public string? Email { get; set; }

    [Column(DataType = DataType.Timestamp, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(DataType = DataType.Bool, DefaultValue = "true")]
    public bool IsActive { get; set; }
}
```

### 2. Use the Repository Pattern

```csharp
// Get the library
var library = context.GetLibrary<PostgreSQL2Library>("cl.postgresql");

// Create a repository
var userRepo = library.GetRepository<User>();

// Insert
var user = new User { Username = "john_doe", Email = "john@example.com" };
var result = await userRepo.InsertAsync(user);

// Get by ID
var fetchedUser = await userRepo.GetByIdAsync(1);

// Get all with caching (300 second TTL)
var allUsers = await userRepo.GetAllAsync(cacheTtl: 300);

// Paginated results
var pagedUsers = await userRepo.GetPagedAsync(page: 1, pageSize: 10);

// Update
user.IsActive = false;
await userRepo.UpdateAsync(user);

// Delete
await userRepo.DeleteAsync(user.Id);

// Count
var totalUsers = await userRepo.CountAsync();
```

### 3. Use the Query Builder

```csharp
var queryBuilder = library.GetQueryBuilder<User>();

// Simple query
var activeUsers = await queryBuilder
    .WhereEquals("IsActive", true)
    .OrderByDesc("CreatedAt")
    .ExecuteAsync();

// Complex query with multiple conditions
var results = await queryBuilder
    .Select("Id", "Username", "Email")
    .Where("IsActive", "=", true)
    .WhereGreaterThan("CreatedAt", new DateTime(2024, 1, 1))
    .OrderBy("Username", SortOrder.Asc)
    .Limit(10)
    .ExecuteAsync();

// Paged results
var paged = await queryBuilder
    .WhereEquals("IsActive", true)
    .ExecutePagedAsync(page: 1, pageSize: 20);

// Count results
var count = await queryBuilder
    .WhereEquals("IsActive", true)
    .CountAsync();

// Get single result
var firstUser = await queryBuilder
    .WhereEquals("Username", "john_doe")
    .FirstOrDefaultAsync();

// Generate SQL for debugging
var sql = queryBuilder
    .Select("*")
    .WhereEquals("IsActive", true)
    .ToSql();
```

### 4. Synchronize Tables

```csharp
var library = context.GetLibrary<PostgreSQL2Library>("cl.postgresql");

// Sync single table
await library.SyncTableAsync<User>(createBackup: true);

// Sync multiple tables
var types = new[] { typeof(User), typeof(Post), typeof(Comment) };
var results = await library.SyncTablesAsync(types, createBackup: true);

// Sync entire namespace
var syncResults = await library.SyncNamespaceAsync(
    "MyApp.Models",
    createBackup: true,
    includeDerivedNamespaces: true
);
```

## Model Attributes

### [TableAttribute]
Defines table properties at the class level:
```csharp
[Table(
    Name = "custom_table_name",      // Optional, defaults to class name
    Schema = "public",                // Optional, defaults to "public"
    Comment = "Table description"     // Optional
)]
public class MyModel { }
```

### [ColumnAttribute]
Defines column properties on properties:
```csharp
[Column(
    DataType = DataType.VarChar,      // Required
    Name = "custom_column",           // Optional, defaults to property name
    Size = 255,                       // For VARCHAR/CHAR
    Precision = 10,                   // For NUMERIC
    Scale = 2,                        // For NUMERIC
    Primary = false,                  // Primary key flag
    AutoIncrement = false,            // Auto-increment flag
    NotNull = false,                  // NOT NULL constraint
    Unique = false,                   // UNIQUE constraint
    Index = false,                    // Create index
    DefaultValue = null,              // Default value like "CURRENT_TIMESTAMP"
    Comment = "Column description",   // Column comment
    OnUpdateCurrentTimestamp = false  // Auto-update on modification
)]
public string? MyColumn { get; set; }
```

### [ForeignKeyAttribute]
Defines foreign key constraints:
```csharp
[ForeignKey(
    ReferenceTable = "users",
    ReferenceColumn = "id",
    OnDelete = ForeignKeyAction.Cascade,
    OnUpdate = ForeignKeyAction.Cascade
)]
public long UserId { get; set; }
```

### [CompositeIndexAttribute]
Creates multi-column indexes:
```csharp
[CompositeIndex("idx_user_email", "UserId", "Email", Unique = true)]
public class UserEmail { }
```

### [IgnoreAttribute]
Skip a property in database operations:
```csharp
[Ignore]
public string ComputedValue { get; set; }
```

## Architecture

### ConnectionManager
Manages database connections with pooling and caching:
- Registers and retrieves configurations
- Handles connection lifecycle
- Tests connections
- Executes with automatic connection management
- Transaction support

### Repository<T>
Generic CRUD operations:
- InsertAsync, GetByIdAsync, GetByColumnAsync
- GetAllAsync, GetPagedAsync
- UpdateAsync, DeleteAsync
- CountAsync
- Built-in caching support

### QueryBuilder<T>
Fluent SQL query construction:
- SELECT, WHERE, ORDER BY, GROUP BY
- JOINs (INNER, LEFT, RIGHT, FULL OUTER)
- Aggregates (COUNT, SUM, AVG, MIN, MAX)
- LIMIT, OFFSET
- ExecuteAsync, ExecutePagedAsync, CountAsync, FirstOrDefaultAsync

### TableSyncService
Schema synchronization:
- SchemaAnalyzer: Generates DDL from models
- BackupManager: Creates backups before changes
- MigrationTracker: Tracks migration history

### TypeConverter
Automatic type conversion between C# and PostgreSQL:
- DateTime conversions
- UUID/GUID handling
- JSON serialization
- Array conversions
- Enum handling
- Decimal precision

## Connection String Format

PostgreSQL connection strings follow this format:
```
Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=password;SSL Mode=Prefer;Pooling=true;
```

## Best Practices

1. **Always use repositories or query builders** instead of raw SQL for type safety
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
- **Batch Operations**: Process multiple records efficiently

## Troubleshooting

### Connection Issues
Check the connection string and ensure PostgreSQL is running:
```json
{
  "host": "localhost",
  "port": 5432,
  "database": "test_db",
  "username": "postgres"
}
```

### Schema Sync Issues
Enable logging to see detailed sync operations:
```json
{
  "enable_logging": true
}
```

### Performance Issues
Check slow query logs:
```json
{
  "log_slow_queries": true,
  "slow_query_threshold": 1000
}
```

## Supported PostgreSQL Versions

- PostgreSQL 12.x and above

## Dependencies

- **Npgsql**: PostgreSQL data provider for .NET
- **CodeLogic**: Framework integration

## License

This library is part of the CodeLogic framework and follows the same licensing.

## Version

**Current Version**: 2.0.0

## Support

For issues, questions, or feature requests, please refer to the CodeLogic documentation or create an issue in the repository.
