# CL.MySQL2 Documentation

Welcome to the official documentation for the CL.MySQL2 library. This library provides a powerful and modern data access layer for MySQL in .NET applications, fully integrated with the CodeLogic framework.

This documentation is split into several sections to help you get started and master the library's features.

## Table of Contents

1.  **[Getting Started](./1-getting-started.md)**
    - Learn how to configure the library, including connection strings and the `mysql.json` file.

2.  **[Defining Models](./2-defining-models.md)**
    - An in-depth guide to using attributes like `[Table]`, `[Column]`, `[ForeignKey]`, and `[ManyToMany]` to map your C# classes to database tables.

3.  **[Repository & CRUD Operations](./3-repository-crud.md)**
    - See examples of how to use the `Repository<T>` for basic Create, Read, Update, and Delete (CRUD) operations.

4.  **[Fluent Query Builder](./4-query-builder.md)**
    - Dive into advanced, type-safe queries using the `QueryBuilder<T>`. Learn about filtering, sorting, pagination, bulk updates, and eager loading (`Include`).

5.  **[Explicit Transaction Management](./5-transactions.md)**
    - Learn how to group multiple database operations into a single, atomic transaction to ensure data integrity.

6.  **[Schema Synchronization](./6-schema-synchronization.md)**
    - Understand how the `TableSyncService` works, including the different `SyncMode` strategies (`Safe`, `Reconstruct`, `Destructive`) and how to enable synchronization on startup.
