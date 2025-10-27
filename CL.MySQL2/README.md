# CL.MySQL2 - MySQL Library for CodeLogic Framework

A fully integrated MySQL database library for the CodeLogic framework, featuring connection pooling, caching, repository pattern, and comprehensive ORM support.

## Features

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

### Advanced Queries with QueryBuilder

The QueryBuilder provides a fluent API for constructing complex SQL queries:

```csharp
// Get query builder
var queryBuilder = mysql2.GetQueryBuilder<User>();

// Simple query
var activeUsers = await queryBuilder
    .Where("status", "=", "active")
    .OrderBy("created_at", SortOrder.Desc)
    .ExecuteAsync();

// Complex filtering
var result = await queryBuilder
    .Where("age", ">=", 18)
    .Where("city", "=", "New York", "AND")
    .WhereLike("email", "%@gmail.com")
    .OrderByDesc("last_login")
    .Limit(50)
    .ExecuteAsync();

// Using helper methods
var products = await queryBuilder
    .WhereGreaterThan("price", 100)
    .WhereLessThan("price", 500)
    .WhereIn("category", "Electronics", "Computers", "Gaming")
    .OrderByAsc("price")
    .ExecuteAsync();

// Pagination
var pagedResult = await queryBuilder
    .Where("active", "=", true)
    .OrderBy("created_at", SortOrder.Desc)
    .ExecutePagedAsync(pageNumber: 2, pageSize: 20);

Console.WriteLine($"Page {pagedResult.Data.PageNumber} of {pagedResult.Data.TotalPages}");
Console.WriteLine($"Total items: {pagedResult.Data.TotalItems}");
foreach (var user in pagedResult.Data.Items)
{
    Console.WriteLine($"User: {user.Username}");
}

// Aggregations
var stats = await queryBuilder
    .Where("status", "=", "completed")
    .GroupBy("user_id")
    .Count("*", "order_count")
    .Sum("total_amount", "total_spent")
    .ExecuteAsync();

// Get single result
var firstUser = await queryBuilder
    .WhereEquals("email", "john@example.com")
    .FirstOrDefaultAsync();

// Get count
var activeCount = await queryBuilder
    .Where("status", "=", "active")
    .CountAsync();

// Joins
var ordersWithUsers = await queryBuilder
    .Select("orders.*", "users.name as user_name", "users.email")
    .LeftJoin("users", "orders.user_id = users.id")
    .Where("orders.status", "=", "pending")
    .ExecuteAsync();

// Range queries
var recentOrders = await queryBuilder
    .WhereBetween("created_at", DateTime.Now.AddDays(-30), DateTime.Now)
    .OrderByDesc("created_at")
    .ExecuteAsync();

// Using Skip/Take (alternative to Offset/Limit)
var secondPage = await queryBuilder
    .OrderBy("id")
    .Skip(20)
    .Take(10)
    .ExecuteAsync();

// Debug: View generated SQL
var query = mysql2.GetQueryBuilder<User>()
    .Where("age", ">", 18)
    .OrderBy("name");
Console.WriteLine(query.ToSql());
// Output: SELECT * FROM `users` WHERE `age` > @p0 ORDER BY `name` ASC

// Non-generic usage
var qb = mysql2.GetQueryBuilder();
var users = await qb.For<User>()
    .Where("active", "=", true)
    .ExecuteAsync();
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

1. **ConnectionManager**: Manages database connections with pooling and caching
2. **Repository<T>**: Generic repository for CRUD operations
3. **QueryBuilder<T>**: Fluent API for building complex SQL queries
4. **TypeConverter**: Handles conversion between C# and MySQL types
5. **DatabaseConfiguration**: Configuration model for database connections

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
- Complete rewrite for CodeLogic 2.0 framework
- Full integration with new logging, configuration, and DI systems
- Enhanced connection management with health checks
- Improved type conversion and error handling
- Support for multiple database connections

## License

Part of the CodeLogic framework by Media2A.
