# CL.MySQL2 - MySQL Library for CodeLogic Framework

A fully integrated MySQL database library for the CodeLogic framework, featuring **type-safe LINQ query support**, connection pooling, caching, repository pattern, and comprehensive ORM support.

## Features

- **Type-Safe LINQ Queries** ⭐ - Compile-time error checking with lambda expressions
- **Full CodeLogic Integration**: Seamlessly integrates with the framework's logging, configuration, and dependency injection systems
- **Connection Management**: Optimized connection pooling with automatic connection lifecycle management
- **Repository Pattern**: Generic repository implementation for type-safe CRUD operations
- **Multiple Database Support**: Configure and manage connections to multiple MySQL databases simultaneously
- **Connection Testing**: Automatic connection health checks during initialization
- **Caching**: Built-in query result caching with configurable TTL
- **Type Safety**: Strong typing with attribute-based model mapping
- **Comprehensive Logging**: Integrated with CodeLogic's logging system for detailed operation tracking

## Installation

Add the project reference to your application:

```xml
<ProjectReference Include="path/to/CL.MySQL2/CL.MySQL2.csproj" />
```

## Configuration

The library uses the CodeLogic configuration system. Create a `mysql.json` file in your `config` directory:

```json
{
  "host": "localhost",
  "port": 3306,
  "database": "your_database",
  "username": "root",
  "password": "your_password",
  "min_pool_size": 5,
  "max_pool_size": 100,
  "connection_timeout": 30,
  "enable_logging": true,
  "enable_caching": true,
  "log_slow_queries": true,
  "slow_query_threshold_ms": 1000
}
```

## Usage

### Defining Models

```csharp
using CL.MySQL2.Models;

[Table(Name = "users")]
public class User
{
    [Column(DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true)]
    public string Username { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Email { get; set; }

    [Column(DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Ignore]
    public string TempData { get; set; } // Not mapped to database
}
```

### Accessing the Library

The library is automatically loaded by the CodeLogic framework. Access it through your application:

```csharp
// Get the MySQL2 library instance
var mysql2 = libraryManager.GetLibrary<MySQL2Library>();

// Get a repository for your model
var userRepository = mysql2.GetRepository<User>();

// Or use a specific database connection
var userRepository = mysql2.GetRepository<User>("SecondaryDB");
```

### CRUD Operations with Repository

```csharp
// Insert
var newUser = new User
{
    Username = "john_doe",
    Email = "john@example.com"
};
var insertResult = await userRepository.InsertAsync(newUser);

if (insertResult.Success)
{
    Console.WriteLine($"User created with ID: {newUser.Id}");
}

// Get by ID
var getResult = await userRepository.GetByIdAsync(1);
if (getResult.Success && getResult.Data != null)
{
    var user = getResult.Data;
    Console.WriteLine($"Found user: {user.Username}");
}

// Get all
var allUsersResult = await userRepository.GetAllAsync(cacheTtl: 300); // Cache for 5 minutes
if (allUsersResult.Success)
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
var queryBuilder = mysql2.GetQueryBuilder<User>();

// Simple WHERE clause (type-safe!)
var activeUsers = await queryBuilder
    .Where(u => u.IsActive == true)
    .ExecuteAsync();

// Multiple conditions with AND
var results = await queryBuilder
    .Where(u => u.IsActive && u.Age > 18)
    .ExecuteAsync();

// OR conditions
var vipUsers = await queryBuilder
    .Where(u => u.Email.EndsWith("@vip.com") || u.Email.EndsWith("@premium.com"))
    .ExecuteAsync();

// Date comparisons
var recentUsers = await queryBuilder
    .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-1))
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
    .ExecutePagedAsync(page: 1, pageSize: 20);

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
    .Where(u => statuses.Contains(u.Status))
    .ExecuteAsync();
```

#### Aggregates & Statistics

```csharp
// Count
var totalActive = await queryBuilder
    .Where(u => u.IsActive)
    .CountAsync();

// Sum
var totalRevenue = await queryBuilder
    .Sum(o => o.Amount, "total_revenue")
    .ExecuteAsync();

// Average
var avgRating = await queryBuilder
    .Avg(o => o.Rating, "avg_rating")
    .ExecuteAsync();

// Min/Max
var stats = await queryBuilder
    .Where(o => o.IsCompleted)
    .Min(o => o.Price, "min_price")
    .Max(o => o.Price, "max_price")
    .ExecuteAsync();
```

#### First or Default

```csharp
var firstUser = await queryBuilder
    .Where(u => u.Email == "john@example.com")
    .FirstOrDefaultAsync();

if (firstUser.Success && firstUser.Data != null)
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
// Output: SELECT * FROM `users` WHERE `IsActive` = @p0 AND `Email` LIKE @p1 ORDER BY `CreatedAt` DESC LIMIT 10
```


### Multiple Database Connections

```csharp
// Register additional database at runtime
var secondaryConfig = new DatabaseConfiguration
{
    ConnectionId = "SecondaryDB",
    Host = "secondary-server.example.com",
    Port = 3306,
    Database = "secondary_database",
    Username = "user",
    Password = "password"
};

mysql2.RegisterDatabase("SecondaryDB", secondaryConfig);

// Use the secondary database
var repository = mysql2.GetRepository<User>("SecondaryDB");
```

### Direct Connection Management

```csharp
// Get connection manager for advanced operations
var connectionManager = mysql2.GetConnectionManager();

// Test a connection
var isConnected = await connectionManager.TestConnectionAsync("Default");

// Get server info
var serverInfo = await connectionManager.GetServerInfoAsync("Default");
Console.WriteLine($"MySQL Version: {serverInfo.Value.Version}");

// Execute custom operations
var result = await connectionManager.ExecuteWithConnectionAsync(async connection =>
{
    using var command = new MySqlCommand("SELECT COUNT(*) FROM users", connection);
    return (int)(long)await command.ExecuteScalarAsync();
}, "Default");
```

## LINQ vs Magic Strings: Before & After

### Before (Magic Strings - ❌ Not Type-Safe)

```csharp
// ❌ Typo won't be caught until runtime!
var users = await queryBuilder
    .Where("IsActiv", "=", true)        // Typo in column name
    .Where("Age", ">", "eighteen")       // Type mismatch
    .OrderByDesc("CretedAt")              // Another typo!
    .ExecuteAsync();
```

### After (LINQ - ✅ Type-Safe!)

```csharp
// ✅ Compiler catches errors immediately!
var users = await queryBuilder
    .Where(u => u.IsActive == true)     // ✓ IntelliSense & compiler check
    .Where(u => u.Age > 18)             // ✓ Type-safe comparison
    .OrderByDescending(u => u.CreatedAt) // ✓ Property rename updates query
    .ExecuteAsync();
```

**Benefits of LINQ:**
- ✅ Compile-time error detection
- ✅ Full IntelliSense support in your IDE
- ✅ Safe refactoring (rename properties and queries update automatically)
- ✅ No magic strings
- ✅ Type-safe comparisons

## Supported Data Types

- Numeric: `TinyInt`, `SmallInt`, `MediumInt`, `Int`, `BigInt`, `Float`, `Double`, `Decimal`
- Date/Time: `DateTime`, `Date`, `Time`, `Timestamp`, `Year`
- String: `Char`, `VarChar`, `Text`, `TinyText`, `MediumText`, `LongText`
- Binary: `Binary`, `VarBinary`, `Blob`, `TinyBlob`, `MediumBlob`, `LongBlob`
- Other: `Json`, `Uuid`, `Bool`, `Enum`, `Set`

## Attributes

- `[Table]`: Define table name and properties
- `[Column]`: Define column properties (type, size, constraints)
- `[ForeignKey]`: Define foreign key relationships
- `[CompositeIndex]`: Create composite indexes
- `[Ignore]`: Exclude property from database operations

## Health Checks

The library implements comprehensive health checks:

```csharp
var healthResult = await mysql2.HealthCheckAsync();

if (healthResult.IsHealthy)
{
    Console.WriteLine($"✓ {healthResult.Message}");
}
else
{
    Console.WriteLine($"✗ {healthResult.Message}");
}
```

## Architecture

### Key Components

1. **ExpressionVisitor**: Converts LINQ expression trees to SQL WHERE conditions ⭐
2. **ConnectionManager**: Manages database connections with pooling and caching
3. **Repository<T>**: Generic repository for CRUD operations
4. **QueryBuilder<T>**: Type-safe fluent API using LINQ expressions
5. **TypeConverter**: Handles conversion between C# and MySQL types
6. **DatabaseConfiguration**: Configuration model for database connections

### Integration Points

- **Logging**: Integrated with `CL.Core.ILogger` for operation logging
- **Configuration**: Uses `CodeLogic.Configuration.ConfigurationManager`
- **Library Lifecycle**: Implements `CL.Core.ILibrary` for framework integration

## Dependencies

- **MySqlConnector**: MySQL database driver
- **CL.Core**: Core CodeLogic library for abstractions
- **CodeLogic**: Main CodeLogic framework

## Version History

### 2.0.0 (Current)
- **Type-Safe LINQ Only** ⭐ - Full LINQ expression support, magic strings removed
- Complete rewrite for CodeLogic 2.0 framework
- Full integration with new logging, configuration, and DI systems
- Enhanced connection management with health checks
- Improved type conversion and error handling
- Support for multiple database connections

## License

Part of the CodeLogic framework by Media2A.
