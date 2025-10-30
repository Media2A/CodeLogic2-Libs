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

    [Column(DataType = DataType.Bool, DefaultValue = "true")]
    public bool IsActive { get; set; }

    [Column(DataType = DataType.Timestamp, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }
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

// Query with LINQ (Type-Safe!)
var activeUsers = await library.GetQueryBuilder<User>()
    .Where(u => u.IsActive == true)
    .OrderByDescending(u => u.CreatedAt)
    .ExecuteAsync();
```

## Common Patterns

### Pattern 1: Simple CRUD
```csharp
var repo = library.GetRepository<User>();

// Create
var newUser = new User { Name = "Alice", Email = "alice@example.com" };
await repo.InsertAsync(newUser);

// Read
var user = await repo.GetByIdAsync(1);

// Update
user.Name = "Alice Smith";
await repo.UpdateAsync(user);

// Delete
await repo.DeleteAsync(1);

// Count
var totalUsers = await repo.CountAsync();
```

### Pattern 2: LINQ Queries - WHERE Clauses
```csharp
var builder = library.GetQueryBuilder<User>();

// Simple condition
var activeUsers = await builder
    .Where(u => u.IsActive == true)
    .ExecuteAsync();

// Multiple conditions with AND
var premiumUsers = await builder
    .Where(u => u.IsActive && u.Name.Contains("Premium"))
    .ExecuteAsync();

// OR conditions
var vipUsers = await builder
    .Where(u => u.Email.EndsWith("@vip.com") || u.Email.EndsWith("@premium.com"))
    .ExecuteAsync();

// Date comparisons
var recentUsers = await builder
    .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-1))
    .ExecuteAsync();
```

### Pattern 3: LINQ Queries - Sorting & Limiting
```csharp
var builder = library.GetQueryBuilder<User>();

// Simple ordering
var sortedUsers = await builder
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .ExecuteAsync();

// Descending order
var newestUsers = await builder
    .OrderByDescending(u => u.CreatedAt)
    .ExecuteAsync();

// Multiple sort criteria
var sorted = await builder
    .OrderBy(u => u.IsActive)
    .ThenByDescending(u => u.CreatedAt)
    .ExecuteAsync();

// Limit results
var topTen = await builder
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.CreatedAt)
    .Take(10)
    .ExecuteAsync();
```

### Pattern 4: Pagination
```csharp
var builder = library.GetQueryBuilder<User>();

// Paginated results
var page = await builder
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.CreatedAt)
    .ToPagedAsync(page: 1, pageSize: 20);

Console.WriteLine($"Total: {page.TotalItems}");
Console.WriteLine($"Pages: {page.TotalPages}");
foreach (var user in page.Items)
{
    Console.WriteLine($"- {user.Name}");
}

// Skip and take alternative
var results = await builder
    .OrderBy(u => u.Id)
    .Skip(20)        // Skip first 20
    .Take(10)        // Get next 10
    .ExecuteAsync();
```

### Pattern 5: String Operations
```csharp
var builder = library.GetQueryBuilder<User>();

// Contains (LIKE with %)
var matchingUsers = await builder
    .Where(u => u.Email.Contains("@company.com"))
    .ExecuteAsync();

// StartsWith (LIKE prefix)
var adminUsers = await builder
    .Where(u => u.Name.StartsWith("Admin"))
    .ExecuteAsync();

// EndsWith (LIKE suffix)
var gmailUsers = await builder
    .Where(u => u.Email.EndsWith("@gmail.com"))
    .ExecuteAsync();

// Combined string operations
var filtered = await builder
    .Where(u => u.Name.Contains("John") && u.Email.EndsWith("@example.com"))
    .ExecuteAsync();
```

### Pattern 6: Collection Filtering (IN clause)
```csharp
var builder = library.GetQueryBuilder<User>();

// Filter by ID list
var userIds = new[] { 1, 2, 3, 4, 5 };
var specificUsers = await builder
    .Where(u => userIds.Contains((int)u.Id))
    .ExecuteAsync();

// Filter by status list
var statuses = new[] { "Active", "Premium", "Trial" };
var statusUsers = await builder
    .Where(u => statuses.Contains(u.Status))
    .ExecuteAsync();
```

### Pattern 7: Aggregates & Statistics
```csharp
var builder = library.GetQueryBuilder<User>();

// Count
var totalActive = await builder
    .Where(u => u.IsActive)
    .CountAsync();

// Sum (if you have numeric columns)
var builder2 = library.GetQueryBuilder<Order>();
var totalRevenue = await builder2
    .Sum(o => o.Amount, "total_revenue")
    .ExecuteAsync();

// Average
var avgRating = await builder2
    .Avg(o => o.Rating, "avg_rating")
    .ExecuteAsync();

// Min/Max
var stats = await builder2
    .Where(o => o.IsCompleted)
    .ExecuteAsync();  // Then use LINQ to Objects for Min/Max
```

### Pattern 8: Transactions
```csharp
await library.GetConnectionManager()
    .ExecuteWithTransactionAsync(async (connection, transaction) =>
    {
        var userRepo = library.GetRepository<User>();
        var postRepo = library.GetRepository<Post>();

        // Multiple operations in single transaction
        var user = new User { Name = "Bob", Email = "bob@example.com" };
        var result = await userRepo.InsertAsync(user);

        var post = new Post { UserId = user.Id, Title = "Hello World" };
        await postRepo.InsertAsync(post);

        // Transaction commits automatically on success
        return true;
    });
```

## LINQ Comparison: Before & After

### Before (Magic Strings - ❌ Not Type-Safe)
```csharp
// ❌ Typo won't be caught until runtime!
var results = await builder
    .Where("IsActiv", "=", true)        // Typo in column name
    .Where("Age", ">", "eighteen")       // Type mismatch
    .OrderByDesc("CretedAt")              // Another typo
    .ExecuteAsync();
```

### After (LINQ - ✅ Type-Safe!)
```csharp
// ✅ Compiler catches errors immediately!
var results = await builder
    .Where(u => u.IsActive == true)      // ✓ IntelliSense helps
    .Where(u => u.Age > 18)              // ✓ Type-safe comparison
    .OrderByDescending(u => u.CreatedAt) // ✓ Compiler checks property
    .ExecuteAsync();
```

**Benefits of LINQ:**
- ✅ Compile-time error detection
- ✅ Full IntelliSense support in your IDE
- ✅ Safe refactoring (rename properties and queries update automatically)
- ✅ No magic strings
- ✅ Type-safe comparisons

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

    [Column(DataType = DataType.Jsonb)]  // Better performance in PostgreSQL
    public string? Value { get; set; }
}
```

## Query Debugging

### View Generated SQL
```csharp
var builder = library.GetQueryBuilder<User>();

var sql = builder
    .Where(u => u.IsActive && u.Email.Contains("@company.com"))
    .OrderByDescending(u => u.CreatedAt)
    .Take(10)
    .ToSql();

Console.WriteLine(sql);
// Output: SELECT * FROM "public"."User" WHERE "IsActive" = @p0 AND "Email" LIKE @p1 ORDER BY "CreatedAt" DESC LIMIT 10
```

### First or Default
```csharp
var firstUser = await library.GetQueryBuilder<User>()
    .Where(u => u.Email == "john@example.com")
    .FirstOrDefaultAsync();

if (firstUser != null)
{
    Console.WriteLine($"Found: {firstUser.Name}");
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

### Issue: Type Safety Errors in LINQ
**Solution**: Ensure property names match column names and use correct types
```csharp
// ✓ Correct - property type matches comparison
public string Email { get; set; }
.Where(u => u.Email == "test@example.com")

// ✗ Wrong - comparison type mismatch
.Where(u => u.Email == 123)  // Compiler error!

// ✓ Correct - property exists with this exact name
.Where(u => u.IsActive == true)

// ✗ Wrong - property typo
.Where(u => u.IsActiv == true)  // Compiler error!
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
**Solution**: Enable caching, use indexes, and check queries
```json
{
  "enable_caching": true,
  "default_cache_ttl": 300,
  "enable_logging": true,
  "log_slow_queries": true,
  "slow_query_threshold": 1000
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

2. **Use LINQ instead of magic strings**
   ```csharp
   .Where(u => u.IsActive)          // ✓ Good - type-safe
   .Where("IsActive", "=", true)    // ✗ Bad - magic strings
   ```

3. **Use caching for read-heavy operations**
   ```csharp
   var user = await repo.GetByIdAsync(1, cacheTtl: 300);  // Cache 5 minutes
   ```

4. **Use transactions for multi-step operations**
   ```csharp
   await connectionManager.ExecuteWithTransactionAsync(async (conn, trans) =>
   {
       // Multiple operations with automatic rollback on error
   });
   ```

5. **Index frequently queried columns**
   ```csharp
   [Column(DataType = DataType.VarChar, Index = true)]
   public string? Email { get; set; }
   ```

6. **Use pagination for large datasets**
   ```csharp
   var page = await builder.ToPagedAsync(1, 20);
   ```

7. **Debug queries with ToSql()**
   ```csharp
   var sql = builder.Where(u => u.IsActive).ToSql();
   Console.WriteLine($"Generated SQL: {sql}");
   ```

## Resources

- [README.md](README.md) - Comprehensive feature documentation
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Architecture and design patterns
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

**Happy coding with type-safe LINQ! 🚀**
