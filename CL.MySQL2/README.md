# CL.MySQL2

A powerful and modern .NET library for MySQL, designed for high performance and ease of use. It features a fluent query builder, a simple repository pattern, and an automatic schema synchronization service.

This library is a component of the CodeLogic application framework.

## Core Features

- **Fluent, Type-Safe Queries:** Write complex database queries using LINQ expressions.
- **Advanced Relationship Management:** Eager load one-to-many, many-to-one, and many-to-many relationships with a simple `.Include()` method.
- **Explicit Transaction Management:** Group multiple operations into a single atomic transaction.
- **High-Performance Bulk Operations:** Insert or update thousands of records efficiently.
- **Automatic Schema Sync:** Keep your database schema perfectly in sync with your C# models with configurable safety modes (`Safe`, `Reconstruct`, `Destructive`).
- **Built-in Connection Pooling:** Robust and reliable connection management.

---

## Documentation

For a complete guide to installation, configuration, and all features, please see the **[Official Documentation](./docs/index.md)**.

The documentation is split into several sections for easy navigation:

- [Getting Started](./docs/1-getting-started.md)
- [Defining Models](./docs/2-defining-models.md)
- [Repository & CRUD Operations](./docs/3-repository-crud.md)
- [Fluent Query Builder](./docs/4-query-builder.md)
- [Explicit Transaction Management](./docs/5-transactions.md)
- [Schema Synchronization](./docs/6-schema-synchronization.md)


## License

Part of the CodeLogic framework by Media2A.
