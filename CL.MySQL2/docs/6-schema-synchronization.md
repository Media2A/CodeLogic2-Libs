# 6. Schema Synchronization

The `TableSyncService` is a powerful feature of CL.MySQL2 that keeps your database schema perfectly in sync with your C# model definitions. It can automatically create tables, add/modify/drop columns, and manage indexes and foreign key constraints.

## Full Schema Awareness

The synchronization service is now fully aware of your entire schema definition, including:

-   **Columns:** Type, size, nullability, default values, auto-increment, character sets, and comments.
-   **Indexes:** Single-column and composite indexes, including uniqueness.
-   **Foreign Keys:** Relationships defined with the `[ForeignKey]` attribute, including `ON DELETE` and `ON UPDATE` actions.

The service compares the state of your models with the state of the database and generates the necessary `ALTER TABLE` statements to make them match.

## Sync Modes

The behavior of the sync service is controlled by the `sync_mode` property in your `mysql.json` configuration. You can choose the strategy that best fits your environment.

-   **`None`**: Disables all automatic schema synchronization. The library will not make any changes to your database schema.

-   **`Safe`** (Default): **Recommended for production.** This mode ensures your schema evolves without risking data loss. It performs the following operations:
    -   Creates new tables.
    -   Adds new columns found in your models.
    -   Modifies existing columns if the change is non-destructive (e.g., increasing a `VARCHAR` size).
    -   Creates, updates, and deletes **indexes** to match your model attributes.
    -   Creates, updates, and deletes **foreign key constraints** to match your model attributes.
    -   It will **never** drop a table or drop a column, even if you remove it from your models.

-   **`Reconstruct`**: **For development.** This mode allows for breaking schema changes while attempting to preserve data. It does everything `Safe` mode does, plus:
    -   If a column modification (e.g., changing a data type) is blocked by a foreign key, it will automatically drop the constraint, perform the change, and then re-add it.
    -   **Use with caution.** This can alter constraints on a live database and is intended for development environments where the schema is still evolving.

-   **`Destructive`**: **For development and testing only.** This is the most aggressive mode and forces the schema to match the model exactly.
    -   If a table's schema doesn't match the model, the service will **drop the entire table and recreate it.**
    -   **WARNING: All data in the dropped table will be permanently lost.**

## Sync on Startup

You can configure the library to run the synchronization service automatically every time your application starts. This is useful for development environments to ensure your database is always up-to-date with your latest model changes.

To enable this, set the following in your `mysql.json`:

```json
{
  "Default": {
    // ... other settings
    "sync_on_startup": true,
    "namespaces_to_sync": [ "YourProject.Models", "YourProject.Data.Entities" ]
  }
}
```

- `sync_on_startup`: Set to `true` to enable.
- `namespaces_to_sync`: Provide a list of C# namespaces where your database models are located. The service will scan these namespaces for classes with the `[Table]` attribute and sync them.