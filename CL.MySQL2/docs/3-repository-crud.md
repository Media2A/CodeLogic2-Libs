# 3. Repository & CRUD Operations

The `Repository<T>` provides a simple, high-level abstraction for performing basic Create, Read, Update, and Delete (CRUD) operations. It's the quickest way to interact with your data.

First, get a repository instance from the main library object:

```csharp
var userRepo = mySqlLibrary.GetRepository<User>();
```

## Create (Insert)

Use `InsertAsync` to save a new entity to the database. The method will automatically update the entity with its new `Id` if it's an auto-incrementing primary key.

```csharp
var newUser = new User { Username = "jane_doe", Email = "jane@example.com" };
var result = await userRepo.InsertAsync(newUser);

Console.WriteLine($"New user created with ID: {result.Data.Id}");
```

## Bulk Create (InsertMany)

Use `InsertManyAsync` to efficiently insert a large number of records in a single operation.

```csharp
var newUsers = new List<User>
{
    new User { Username = "user1", Email = "user1@example.com" },
    new User { Username = "user2", Email = "user2@example.com" }
};
var result = await userRepo.InsertManyAsync(newUsers);

Console.WriteLine($"Successfully inserted {result.Data} users.");
```

## Read

### Get by ID

Use `GetByIdAsync` to retrieve a single entity by its primary key.

```csharp
var userResult = await userRepo.GetByIdAsync(1);
if (userResult.Success && userResult.Data != null) 
{
    Console.WriteLine($"Found user: {userResult.Data.Username}");
}
```

### Get All

Use `GetAllAsync` to retrieve all records from a table.

```csharp
var allUsersResult = await userRepo.GetAllAsync();
if (allUsersResult.Success) 
{
    Console.WriteLine($"Total users: {allUsersResult.Data.Count}");
}
```

## Update

Use `UpdateAsync` to save changes to an existing entity. You should retrieve the entity first, modify its properties, and then pass it to the `UpdateAsync` method.

```csharp
var userResult = await userRepo.GetByIdAsync(1);
if (userResult.Success && userResult.Data != null) 
{
    var user = userResult.Data;
    user.Email = "new_email@example.com";

    await userRepo.UpdateAsync(user);
}
```

## Delete

Use `DeleteAsync` to remove a record from the database by its primary key.

```csharp
await userRepo.DeleteAsync(1);
```
