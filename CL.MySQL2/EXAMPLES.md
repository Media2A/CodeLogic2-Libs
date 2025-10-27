# CL.MySQL2 - Usage Examples

This document provides comprehensive examples of using the CL.MySQL2 library with the CodeLogic framework.

## Table of Contents

- [Basic Setup](#basic-setup)
- [Repository Pattern](#repository-pattern)
- [QueryBuilder - Basic Queries](#querybuilder---basic-queries)
- [QueryBuilder - Filtering](#querybuilder---filtering)
- [QueryBuilder - Pagination](#querybuilder---pagination)
- [QueryBuilder - Aggregations](#querybuilder---aggregations)
- [QueryBuilder - Joins](#querybuilder---joins)
- [Multiple Databases](#multiple-databases)
- [Advanced Scenarios](#advanced-scenarios)

## Basic Setup

### Define Your Models

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

    [Column(DataType = DataType.Int, NotNull = true)]
    public int Age { get; set; }

    [Column(DataType = DataType.VarChar, Size = 100)]
    public string City { get; set; }

    [Column(DataType = DataType.VarChar, Size = 20)]
    public string Status { get; set; }

    [Column(DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(DataType = DataType.DateTime)]
    public DateTime? LastLogin { get; set; }
}

[Table(Name = "orders")]
public class Order
{
    [Column(DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(DataType = DataType.Int, NotNull = true)]
    [ForeignKey(ReferenceTable = "users", ReferenceColumn = "id")]
    public int UserId { get; set; }

    [Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)]
    public decimal TotalAmount { get; set; }

    [Column(DataType = DataType.VarChar, Size = 50)]
    public string Status { get; set; }

    [Column(DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }
}
```

## Repository Pattern

### Basic CRUD Operations

```csharp
var mysql2 = libraryManager.GetLibrary<MySQL2Library>();
var userRepo = mysql2.GetRepository<User>();

// CREATE
var newUser = new User
{
    Username = "alice",
    Email = "alice@example.com",
    Age = 28,
    City = "San Francisco",
    Status = "active"
};

var createResult = await userRepo.InsertAsync(newUser);
if (createResult.Success)
{
    Console.WriteLine($"Created user with ID: {newUser.Id}");
}

// READ by ID
var readResult = await userRepo.GetByIdAsync(newUser.Id);
if (readResult.Success && readResult.Data != null)
{
    Console.WriteLine($"Found: {readResult.Data.Username}");
}

// READ by column
var byEmail = await userRepo.GetByColumnAsync("email", "alice@example.com");

// READ all (with caching)
var allUsers = await userRepo.GetAllAsync(cacheTtl: 300); // Cache for 5 minutes

// UPDATE
newUser.City = "Los Angeles";
var updateResult = await userRepo.UpdateAsync(newUser);
Console.WriteLine($"Updated {updateResult.RowsAffected} row(s)");

// DELETE
var deleteResult = await userRepo.DeleteAsync(newUser.Id);
Console.WriteLine($"Deleted {deleteResult.Data} row(s)");
```

## QueryBuilder - Basic Queries

### Simple Selections

```csharp
var qb = mysql2.GetQueryBuilder<User>();

// Select all
var allUsers = await qb.ExecuteAsync();

// Select with specific columns
var result = await qb
    .Select("id", "username", "email")
    .ExecuteAsync();

// Single result
var user = await qb
    .WhereEquals("username", "alice")
    .FirstOrDefaultAsync();

// Check if user exists and handle result
if (user.Success && user.Data != null)
{
    Console.WriteLine($"Found user: {user.Data.Email}");
}
else
{
    Console.WriteLine("User not found");
}
```

### Ordering Results

```csharp
// Order ascending
var ascending = await qb
    .OrderByAsc("username")
    .ExecuteAsync();

// Order descending
var descending = await qb
    .OrderByDesc("created_at")
    .ExecuteAsync();

// Multiple order clauses
var multiOrder = await qb
    .OrderBy("city", SortOrder.Asc)
    .OrderBy("age", SortOrder.Desc)
    .ExecuteAsync();
```

### Limiting Results

```csharp
// Get first 10 users
var top10 = await qb
    .Limit(10)
    .ExecuteAsync();

// Get users 11-20 (pagination)
var nextPage = await qb
    .Offset(10)
    .Limit(10)
    .ExecuteAsync();

// Alternative: Skip/Take
var skipTake = await qb
    .Skip(10)
    .Take(10)
    .ExecuteAsync();
```

## QueryBuilder - Filtering

### Basic WHERE Clauses

```csharp
// Equals
var active = await qb
    .WhereEquals("status", "active")
    .ExecuteAsync();

// Generic where with operator
var adults = await qb
    .Where("age", ">=", 18)
    .ExecuteAsync();

// Multiple conditions (AND)
var filtered = await qb
    .Where("age", ">=", 18)
    .Where("city", "=", "New York")
    .ExecuteAsync();

// OR conditions
var either = await qb
    .Where("city", "=", "New York", "AND")
    .Where("city", "=", "Los Angeles", "OR")
    .ExecuteAsync();
```

### Comparison Operators

```csharp
// Greater than
var older = await qb
    .WhereGreaterThan("age", 30)
    .ExecuteAsync();

// Less than
var younger = await qb
    .WhereLessThan("age", 25)
    .ExecuteAsync();

// Between
var ageRange = await qb
    .WhereBetween("age", 25, 35)
    .ExecuteAsync();

// Date range
var recent = await qb
    .WhereBetween("created_at",
        DateTime.Now.AddDays(-30),
        DateTime.Now)
    .ExecuteAsync();
```

### Pattern Matching

```csharp
// LIKE - starts with
var startsWithA = await qb
    .WhereLike("username", "a%")
    .ExecuteAsync();

// LIKE - ends with
var gmail = await qb
    .WhereLike("email", "%@gmail.com")
    .ExecuteAsync();

// LIKE - contains
var contains = await qb
    .WhereLike("username", "%john%")
    .ExecuteAsync();
```

### IN Clauses

```csharp
// Multiple values
var specificCities = await qb
    .WhereIn("city", "New York", "Los Angeles", "Chicago")
    .ExecuteAsync();

// With IDs
var specificUsers = await qb
    .WhereIn("id", 1, 5, 10, 15)
    .ExecuteAsync();
```

## QueryBuilder - Pagination

### Basic Pagination

```csharp
// Page 1 (20 items per page)
var page1 = await qb
    .OrderBy("created_at", SortOrder.Desc)
    .ExecutePagedAsync(pageNumber: 1, pageSize: 20);

if (page1.Success)
{
    var paged = page1.Data;
    Console.WriteLine($"Page {paged.PageNumber} of {paged.TotalPages}");
    Console.WriteLine($"Total items: {paged.TotalItems}");
    Console.WriteLine($"Has next page: {paged.HasNextPage}");
    Console.WriteLine($"Has previous page: {paged.HasPreviousPage}");

    foreach (var user in paged.Items)
    {
        Console.WriteLine($"- {user.Username}");
    }
}
```

### Filtered Pagination

```csharp
// Active users, paginated
var activePaged = await qb
    .WhereEquals("status", "active")
    .OrderByDesc("last_login")
    .ExecutePagedAsync(pageNumber: 2, pageSize: 50);
```

### Search with Pagination

```csharp
public async Task<PagedResult<User>> SearchUsers(string searchTerm, int page, int pageSize)
{
    var qb = mysql2.GetQueryBuilder<User>();

    var result = await qb
        .WhereLike("username", $"%{searchTerm}%")
        .OrderBy("username")
        .ExecutePagedAsync(page, pageSize);

    return result.Data;
}
```

## QueryBuilder - Aggregations

### Count

```csharp
// Count all
var totalCount = await qb.CountAsync();
Console.WriteLine($"Total users: {totalCount.Data}");

// Count with filter
var activeCount = await qb
    .WhereEquals("status", "active")
    .CountAsync();
Console.WriteLine($"Active users: {activeCount.Data}");
```

### Grouping and Aggregates

```csharp
// Group by city with counts
var cityStats = await qb
    .GroupBy("city")
    .Count("*", "user_count")
    .ExecuteAsync();

foreach (var stat in cityStats.Data)
{
    // Access aggregated results
    Console.WriteLine($"City: {stat.City}");
}

// Multiple aggregates
var orderStats = mysql2.GetQueryBuilder<Order>();
var stats = await orderStats
    .GroupBy("status")
    .Count("*", "order_count")
    .Sum("total_amount", "total_revenue")
    .Avg("total_amount", "avg_order_value")
    .Min("total_amount", "min_order")
    .Max("total_amount", "max_order")
    .ExecuteAsync();
```

## QueryBuilder - Joins

### Inner Join

```csharp
// Users with their orders
var usersWithOrders = await qb
    .Select("users.*", "COUNT(orders.id) as order_count")
    .InnerJoin("orders", "users.id = orders.user_id")
    .GroupBy("users.id")
    .ExecuteAsync();
```

### Left Join

```csharp
// All users, including those without orders
var allUsersOrders = await qb
    .Select("users.*", "orders.id as order_id", "orders.total_amount")
    .LeftJoin("orders", "users.id = orders.user_id")
    .ExecuteAsync();
```

### Multiple Joins

```csharp
// Complex join example
var complex = await qb
    .Select("users.username", "orders.id as order_id", "products.name as product_name")
    .LeftJoin("orders", "users.id = orders.user_id")
    .LeftJoin("order_items", "orders.id = order_items.order_id")
    .LeftJoin("products", "order_items.product_id = products.id")
    .Where("users.status", "=", "active")
    .ExecuteAsync();
```

## Multiple Databases

### Configure Multiple Connections

```csharp
// Primary database (configured in mysql.json)
var primaryRepo = mysql2.GetRepository<User>("Default");

// Register secondary database
var analyticsConfig = new DatabaseConfiguration
{
    ConnectionId = "Analytics",
    Host = "analytics-db.example.com",
    Port = 3306,
    Database = "analytics",
    Username = "analytics_user",
    Password = "secure_password"
};
mysql2.RegisterDatabase("Analytics", analyticsConfig);

// Use secondary database
var analyticsRepo = mysql2.GetRepository<AnalyticsEvent>("Analytics");
var analyticsQb = mysql2.GetQueryBuilder<AnalyticsEvent>("Analytics");
```

### Query Different Databases

```csharp
// Query primary database
var primaryUsers = await mysql2.GetQueryBuilder<User>("Default")
    .WhereEquals("status", "active")
    .ExecuteAsync();

// Query analytics database
var events = await mysql2.GetQueryBuilder<AnalyticsEvent>("Analytics")
    .WhereBetween("timestamp", DateTime.Now.AddHours(-1), DateTime.Now)
    .ExecuteAsync();
```

## Advanced Scenarios

### Dynamic Query Building

```csharp
public async Task<List<User>> SearchUsers(UserSearchCriteria criteria)
{
    var qb = mysql2.GetQueryBuilder<User>();

    // Start with base query
    qb.OrderBy("username");

    // Conditionally add filters
    if (!string.IsNullOrEmpty(criteria.Username))
        qb.WhereLike("username", $"%{criteria.Username}%");

    if (!string.IsNullOrEmpty(criteria.Email))
        qb.WhereLike("email", $"%{criteria.Email}%");

    if (criteria.MinAge.HasValue)
        qb.WhereGreaterThan("age", criteria.MinAge.Value);

    if (criteria.MaxAge.HasValue)
        qb.WhereLessThan("age", criteria.MaxAge.Value);

    if (criteria.Cities?.Any() == true)
        qb.WhereIn("city", criteria.Cities.ToArray());

    if (!string.IsNullOrEmpty(criteria.Status))
        qb.WhereEquals("status", criteria.Status);

    // Apply pagination if specified
    if (criteria.PageSize > 0)
    {
        qb.Limit(criteria.PageSize);
        if (criteria.Page > 1)
            qb.Offset((criteria.Page - 1) * criteria.PageSize);
    }

    var result = await qb.ExecuteAsync();
    return result.Data ?? new List<User>();
}
```

### Debugging Queries

```csharp
// View the SQL that will be executed
var qb = mysql2.GetQueryBuilder<User>();
qb.Where("age", ">", 18)
  .OrderBy("username")
  .Limit(10);

string sql = qb.ToSql();
Console.WriteLine($"SQL: {sql}");
// Output: SELECT * FROM `users` WHERE `age` > @p0 ORDER BY `username` ASC LIMIT 10

// Execute the query
var result = await qb.ExecuteAsync();
```

### Reusable Query Components

```csharp
// Create base query
var activeUsersBase = mysql2.GetQueryBuilder<User>()
    .WhereEquals("status", "active");

// Clone and extend for different purposes
var recentActive = activeUsersBase
    .WhereBetween("last_login", DateTime.Now.AddDays(-7), DateTime.Now)
    .OrderByDesc("last_login");

var topActive = activeUsersBase
    .OrderByDesc("created_at")
    .Limit(100);
```

### Performance Monitoring

```csharp
// The library automatically logs slow queries based on configuration
// config/mysql.json:
// {
//   "log_slow_queries": true,
//   "slow_query_threshold_ms": 1000
// }

// Queries taking longer than 1000ms will be logged automatically
var result = await qb
    .Join("orders", "users.id = orders.user_id")
    .Join("products", "orders.product_id = products.id")
    .ExecuteAsync();

// Check logs for slow query warnings
```

### Transaction Support

```csharp
var connectionManager = mysql2.GetConnectionManager();

var result = await connectionManager.ExecuteWithTransactionAsync(async (connection, transaction) =>
{
    // Create user
    using var userCmd = new MySqlCommand(
        "INSERT INTO users (username, email) VALUES (@username, @email)",
        connection,
        transaction);
    userCmd.Parameters.AddWithValue("@username", "newuser");
    userCmd.Parameters.AddWithValue("@email", "new@example.com");
    await userCmd.ExecuteNonQueryAsync();

    // Create initial order
    using var orderCmd = new MySqlCommand(
        "INSERT INTO orders (user_id, total_amount) VALUES (LAST_INSERT_ID(), @amount)",
        connection,
        transaction);
    orderCmd.Parameters.AddWithValue("@amount", 100.00m);
    await orderCmd.ExecuteNonQueryAsync();

    return true;
});

// Transaction is automatically committed on success or rolled back on error
```

### Health Monitoring

```csharp
// Check database health
var health = await mysql2.HealthCheckAsync();

if (health.IsHealthy)
{
    Console.WriteLine($"✓ {health.Message}");
}
else
{
    Console.WriteLine($"✗ Database issues: {health.Message}");
    // Take corrective action
}
```

## Best Practices

1. **Use QueryBuilder for complex queries**: It provides better readability and type safety
2. **Use Repository for simple CRUD**: It's optimized for basic operations
3. **Enable caching for read-heavy operations**: Set `cacheTtl` parameter
4. **Use pagination for large result sets**: Prevents memory issues
5. **Monitor slow queries**: Configure threshold in `mysql.json`
6. **Use transactions for multi-step operations**: Ensures data consistency
7. **Test database connections on startup**: The library does this automatically
8. **Use named connections for multiple databases**: Better organization and clarity
9. **Debug with `ToSql()`**: View generated SQL before execution
10. **Handle operation results properly**: Always check `Success` property

## Common Patterns

### Repository + QueryBuilder Combo

```csharp
// Use Repository for simple operations
var user = await userRepo.GetByIdAsync(1);

// Use QueryBuilder for complex queries
var recentActiveUsers = await qb
    .WhereEquals("status", "active")
    .WhereBetween("last_login", DateTime.Now.AddDays(-30), DateTime.Now)
    .OrderByDesc("last_login")
    .Limit(50)
    .ExecuteAsync();
```

### Service Layer Pattern

```csharp
public class UserService
{
    private readonly MySQL2Library _mysql2;
    private readonly Repository<User> _userRepo;

    public UserService(MySQL2Library mysql2)
    {
        _mysql2 = mysql2;
        _userRepo = mysql2.GetRepository<User>();
    }

    public async Task<User?> GetUserAsync(int id)
    {
        var result = await _userRepo.GetByIdAsync(id);
        return result.Data;
    }

    public async Task<PagedResult<User>> SearchUsersAsync(string query, int page, int pageSize)
    {
        var qb = _mysql2.GetQueryBuilder<User>();
        var result = await qb
            .WhereLike("username", $"%{query}%")
            .OrderBy("username")
            .ExecutePagedAsync(page, pageSize);

        return result.Data;
    }
}
```
