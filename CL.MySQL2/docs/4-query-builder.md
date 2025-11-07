# 4. Fluent Query Builder

For queries that go beyond simple CRUD operations, the `QueryBuilder<T>` provides a powerful, type-safe, and fluent interface using LINQ expressions. This allows you to build complex SQL queries with compile-time safety and full IntelliSense.

Get a `QueryBuilder` instance from the main library object:

```csharp
var query = mySqlLibrary.GetQueryBuilder<User>();
```

## Filtering with `Where`

You can chain multiple `Where` clauses. They will be combined with `AND` by default. You can also use `||` (`OR`) within a single `Where` expression.

```csharp
// Find all active admins
var admins = await query
    .Where(u => u.IsAdmin == true && u.IsActive == true)
    .ExecuteAsync();

// Find users with a GMail or Outlook email using OR
var emailUsers = await query
    .Where(u => u.Email.EndsWith("@gmail.com") || u.Email.EndsWith("@outlook.com"))
    .ExecuteAsync();

// Find users within a list of IDs using Contains() (translates to SQL `IN`)
var userIds = new[] { 10, 25, 37 };
var specificUsers = await query
    .Where(u => userIds.Contains(u.Id))
    .ExecuteAsync();
```

## Sorting and Paginating

Use `OrderBy`, `OrderByDescending`, `Skip`, and `Take` to sort and paginate your results.

```csharp
// Get the 10 newest users
var newestUsers = await query
    .OrderByDescending(u => u.CreatedAt)
    .Take(10) // Alias for Limit()
    .ExecuteAsync();

// Get the 3rd page of 50 active users, sorted by username
var pagedUsers = await mySqlLibrary.GetQueryBuilder<User>()
    .Where(u => u.IsActive == true)
    .OrderBy(u => u.Username)
    .ExecutePagedAsync(page: 3, pageSize: 50);
```

## Eager Loading with `Include()`

Use `Include()` to load related data in a single query, avoiding the "N+1" problem. This works for one-to-many, many-to-one, and many-to-many relationships.

```csharp
// Get a blog and all of its posts (one-to-many)
var blogWithPosts = await mySqlLibrary.GetQueryBuilder<Blog>()
    .Where(b => b.Id == 1)
    .Include(b => b.Posts)
    .ExecuteSingleAsync();

// Get a post and its related tags (many-to-many)
var postWithTags = await mySqlLibrary.GetQueryBuilder<Post>()
    .Where(p => p.Id == 42)
    .Include(p => p.Tags)
    .ExecuteSingleAsync();
```

## Bulk Updates

Update multiple records matching a `WHERE` clause without fetching them first. This is highly efficient for batch operations.

```csharp
// Deactivate all users who haven't logged in for 90 days
var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

var result = await mySqlLibrary.GetQueryBuilder<User>()
    .Where(u => u.LastLogin < ninetyDaysAgo && u.IsActive == true)
    .UpdateAsync(new { IsActive = false, StatusMessage = "Deactivated due to inactivity" });

Console.WriteLine($"Deactivated {result.Data} user(s).");
```
