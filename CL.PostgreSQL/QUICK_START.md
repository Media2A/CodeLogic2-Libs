# CL.PostgreSQL - Quick Start Guide

## 5-Minute Setup

### Step 1: Install PostgreSQL
Ensure PostgreSQL is running on your system (default port 5432).

### Step 2: Create Database Configuration
Create `config/postgresql.json` in your project root:

```json
{
  "Default": {
    "enabled": true,
    "host": "localhost",
    "port": 5432,
    "database": "myapp_db",
    "username": "postgres",
    "password": "your_password",
    "enable_auto_sync": true
  }
}
```

### Step 3: Define Your Models
```csharp
using CL.PostgreSQL.Models;

[Table(Schema = "public")]
public class User
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string? Name { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true, Unique = true)]
    public string? Email { get; set; }
}
```

### Step 4: Use in Your Application
```csharp
// Get library from CodeLogic
var library = context.GetLibrary<PostgreSQL2Library>("cl.postgresql");

// Sync table schema
await library.SyncTableAsync<User>(createBackup: true);

// Create repository
var userRepo = library.GetRepository<User>();

// Insert
var user = new User { Name = "John", Email = "john@example.com" };
var result = await userRepo.InsertAsync(user);

// Query
var allUsers = await userRepo.GetAllAsync();

// Using QueryBuilder
var activeUsers = await library.GetQueryBuilder<User>()
    .WhereEquals("IsActive", true)
    .OrderByDesc("CreatedAt")
    .ExecuteAsync();
```

## Common Patterns

### Pattern 1: Simple CRUD
```csharp
var repo = library.GetRepository<User>();

// Create
var newUser = new User { Name = "Alice" };
await repo.InsertAsync(newUser);

// Read
var user = await repo.GetByIdAsync(1);

// Update
user.Name = "Alice Smith";
await repo.UpdateAsync(user);

// Delete
await repo.DeleteAsync(1);
```

### Pattern 2: Complex Queries
```csharp
var builder = library.GetQueryBuilder<User>();

var results = await builder
    .Select("Id", "Name", "Email")
    .Where("CreatedAt", ">", DateTime.Now.AddMonths(-1))
    .WhereIn("Status", "Active", "Premium")
    .OrderByDesc("CreatedAt")
    .Limit(10)
    .ExecuteAsync();
```

### Pattern 3: Pagination
```csharp
var builder = library.GetQueryBuilder<User>();

var page = await builder
    .WhereEquals("IsActive", true)
    .ExecutePagedAsync(page: 1, pageSize: 20);

Console.WriteLine($"Total: {page.TotalItems}");
Console.WriteLine($"Pages: {page.TotalPages}");
foreach (var user in page.Items)
{
    Console.WriteLine($"- {user.Name}");
}
```

### Pattern 4: Aggregates & Grouping
```csharp
var stats = await library.GetQueryBuilder<User>()
    .Count("*", "total_users")
    .Sum("TotalSpent", "revenue")
    .Avg("Rating", "avg_rating")
    .GroupBy("Country")
    .ExecuteAsync();
```

### Pattern 5: Transactions
```csharp
await library.GetConnectionManager()
    .ExecuteWithTransactionAsync(async (connection, transaction) =>
    {
        // Multiple operations in single transaction
        var user = new User { Name = "Bob" };
        var result = await userRepo.InsertAsync(user);

        var post = new Post { UserId = user.Id, Title = "Hello" };
        await postRepo.InsertAsync(post);

        // Transaction commits automatically
        return true;
    });
```

## Model Definition Patterns

### Pattern 1: Basic Model
```csharp
[Table]
public class Product
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string? Name { get; set; }

    [Column(DataType = DataType.Numeric, Precision = 10, Scale = 2)]
    public decimal Price { get; set; }
}
```

### Pattern 2: With Timestamps
```csharp
[Table]
public class Article
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 500, NotNull = true)]
    public string? Title { get; set; }

    [Column(DataType = DataType.Timestamp, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(DataType = DataType.Timestamp, OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
```

### Pattern 3: With Relationships
```csharp
[Table]
public class BlogPost
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.BigInt, NotNull = true)]
    [ForeignKey(ReferenceTable = "user", ReferenceColumn = "id", OnDelete = ForeignKeyAction.Cascade)]
    public long UserId { get; set; }

    [Column(DataType = DataType.VarChar, Size = 500, NotNull = true)]
    public string? Title { get; set; }
}
```

### Pattern 4: With Indexes
```csharp
[Table]
[CompositeIndex("idx_user_email", "UserId", "Email", Unique = true)]
public class EmailLog
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.BigInt, Index = true)]
    public long UserId { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255, Index = true)]
    public string? Email { get; set; }
}
```

### Pattern 5: With JSON Storage
```csharp
[Table]
public class Configuration
{
    [Column(DataType = DataType.BigInt, Primary = true, AutoIncrement = true)]
    public long Id { get; set; }

    [Column(DataType = DataType.VarChar, Size = 255)]
    public string? Key { get; set; }

    [Column(DataType = DataType.Jsonb)]  // Better performance
    public string? Value { get; set; }
}
```

## Troubleshooting

### Issue: Connection Failed
**Solution**: Check config/postgresql.json
```json
{
  "host": "localhost",
  "port": 5432,
  "username": "postgres",
  "password": "correct_password"
}
```

### Issue: Table Not Found
**Solution**: Enable auto-sync and sync table
```csharp
await library.SyncTableAsync<User>();
```

### Issue: Type Conversion Error
**Solution**: Verify column DataType matches property type
```csharp
// ✓ Correct
[Column(DataType = DataType.VarChar, Size = 255)]
public string? Name { get; set; }

// ✗ Wrong
[Column(DataType = DataType.Int)]
public string? Name { get; set; }
```

### Issue: Performance Issues
**Solution**: Enable caching and check indexes
```json
{
  "enable_caching": true,
  "default_cache_ttl": 300
}
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| host | localhost | PostgreSQL server host |
| port | 5432 | PostgreSQL server port |
| database | (required) | Database name |
| username | (required) | Database user |
| password | (required) | Database password |
| min_pool_size | 5 | Minimum connection pool size |
| max_pool_size | 100 | Maximum connection pool size |
| connection_timeout | 30 | Connection timeout (seconds) |
| command_timeout | 30 | Command timeout (seconds) |
| enable_caching | true | Enable query result caching |
| default_cache_ttl | 300 | Cache TTL in seconds |
| enable_auto_sync | true | Automatically sync tables |
| enable_logging | false | Enable detailed logging |
| log_slow_queries | true | Log queries exceeding threshold |
| slow_query_threshold | 1000 | Slow query threshold (ms) |

## Advanced Usage

### Custom Connection at Runtime
```csharp
var config = new DatabaseConfiguration
{
    ConnectionId = "Analytics",
    Host = "analytics.example.com",
    Port = 5432,
    Database = "analytics_db",
    Username = "analyst"
};

library.RegisterDatabase("Analytics", config);
var analyticsRepo = library.GetRepository<User>("Analytics");
```

### Namespace Synchronization
```csharp
// Sync all tables in a namespace
var results = await library.SyncNamespaceAsync(
    "MyApp.Models",
    connectionId: "Default",
    createBackup: true,
    includeDerivedNamespaces: true
);

foreach (var (table, success) in results)
{
    Console.WriteLine($"{table}: {(success ? "✓" : "✗")}");
}
```

### Query Debugging
```csharp
var builder = library.GetQueryBuilder<User>();

var sql = builder
    .Select("Id", "Name")
    .WhereEquals("IsActive", true)
    .OrderByDesc("CreatedAt")
    .ToSql();

Console.WriteLine(sql);
// Output: SELECT "Id", "Name" FROM "public"."User" WHERE "IsActive" = @p0 ORDER BY "CreatedAt" DESC
```

### Health Checks
```csharp
var health = await library.HealthCheckAsync();
Console.WriteLine($"Status: {health.Status}");
Console.WriteLine($"Message: {health.Message}");

if (!health.IsHealthy)
{
    Console.WriteLine("Database is down!");
}
```

## Best Practices

1. **Always use async/await**
   ```csharp
   await repo.InsertAsync(entity);  // ✓ Good
   repo.InsertAsync(entity).Wait();  // ✗ Bad (blocks thread)
   ```

2. **Use caching for read-heavy operations**
   ```csharp
   var user = await repo.GetByIdAsync(1, cacheTtl: 300);  // Cache 5 minutes
   ```

3. **Use transactions for multi-step operations**
   ```csharp
   await connectionManager.ExecuteWithTransactionAsync(async (conn, trans) =>
   {
       // Multiple operations
   });
   ```

4. **Index frequently queried columns**
   ```csharp
   [Column(DataType = DataType.VarChar, Index = true)]
   public string? Email { get; set; }
   ```

5. **Use pagination for large datasets**
   ```csharp
   var page = await builder.ExecutePagedAsync(1, 20);
   ```

## Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [CodeLogic Framework](https://github.com/your-org/codelogic)
- [CL.PostgreSQL GitHub](https://github.com/your-org/cl-postgresql)

## Support

For issues or questions:
1. Check the [README.md](README.md)
2. Review [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
3. Check troubleshooting section above
4. Create an issue on GitHub

---

**Happy coding! 🚀**
