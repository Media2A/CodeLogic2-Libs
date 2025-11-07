# 5. Explicit Transaction Management

CL.MySQL2 supports explicit transaction management, allowing you to group multiple database operations into a single atomic unit. This ensures that either all operations succeed and are committed, or if any fail, all are rolled back, maintaining data integrity.

## Usage

Use the `BeginTransactionAsync()` method on the `MySQL2Library` instance to start a new transaction. This method returns a `TransactionScope` object, which should be used within an `await using` statement to ensure proper disposal and automatic rollback on failure.

When operating within a transaction, you must obtain `Repository` and `QueryBuilder` instances by passing the `TransactionScope` object to the special `GetRepository()` and `GetQueryBuilder()` overloads. These transactional instances will execute all their operations within the context of that single transaction.

### Example

Imagine you want to transfer money from one account to another. You need to debit one account and credit another. These two operations must happen together or not at all.

```csharp
// Assume Account model exists
[Table(Name = "accounts")]
public class Account
{
    [Column(Primary = true)]
    public int Id { get; set; }

    [Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)]
    public decimal Balance { get; set; }
}

// --- Transactional Logic ---

var fromAccountId = 1;
var toAccountId = 2;
var amountToTransfer = 100.00m;

// Start a transaction
await using (var transaction = await mySqlLibrary.BeginTransactionAsync())
{
    try
    {
        // Get a transactional repository
        var accountRepo = mySqlLibrary.GetRepository<Account>(transaction);

        // 1. Debit the 'from' account
        var fromAccount = (await accountRepo.GetByIdAsync(fromAccountId)).Data;
        if (fromAccount == null || fromAccount.Balance < amountToTransfer)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }
        fromAccount.Balance -= amountToTransfer;
        await accountRepo.UpdateAsync(fromAccount);

        // 2. Credit the 'to' account
        var toAccount = (await accountRepo.GetByIdAsync(toAccountId)).Data;
        if (toAccount == null)
        {
            throw new InvalidOperationException("Recipient account not found.");
        }
        toAccount.Balance += amountToTransfer;
        await accountRepo.UpdateAsync(toAccount);

        // If both operations succeed, commit the transaction
        await transaction.CommitAsync();
        Console.WriteLine("Transfer successful!");
    }
    catch (Exception ex)
    {
        // An error occurred. The 'await using' statement ensures that
        // TransactionScope.DisposeAsync() is called, which will automatically
        // roll back the transaction if it hasn't been explicitly committed.
        Console.WriteLine($"Transaction failed: {ex.Message}. Rolling back changes.");
    }
}
```
