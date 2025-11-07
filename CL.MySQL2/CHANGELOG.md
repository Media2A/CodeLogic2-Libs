# CL.MySQL2 Changelog

## Version 2.2.0 - Robust Sync & Transaction Control

### Overview
This version delivers a completely overhauled `TableSyncService` that is now fully schema-aware, and introduces explicit transaction management for fine-grained database control. The documentation has also been completely restructured for clarity.

### New Features

#### Schema Synchronization (`TableSyncService`)
- ✅ **Full Schema Synchronization**: The sync service now manages the complete lifecycle of columns, indexes, and foreign keys.
- ✅ **Comprehensive Column Comparison**: Detects changes in data type, size, nullability, default values, auto-increment status, character sets, and comments.
- ✅ **Full Index Synchronization**: Automatically adds, drops, and modifies single-column and composite indexes to perfectly match model attributes (`[Index]`, `[Unique]`, `[CompositeIndex]`).
- ✅ **Full Foreign Key Synchronization**: Automatically adds, drops, and modifies foreign key constraints to match `[ForeignKey]` attributes, including `OnDelete` and `OnUpdate` actions.

#### Transaction Management
- ✅ **Explicit Transactions**: New `BeginTransactionAsync()` method on the `MySQL2Library` returns a disposable `TransactionScope` object to manage the transaction lifecycle.
- ✅ **Transactional Operations**: `GetRepository()` and `GetQueryBuilder()` now have overloads that accept a `TransactionScope`, ensuring all operations performed with the resulting service occur within that single transaction.

#### Documentation
- ✅ **Restructured Documentation**: The monolithic `Documentation.md` has been replaced with a structured `docs/` directory, with each file focusing on a specific feature (e.g., `6-schema-synchronization.md`).
- ✅ **Updated README**: The main `README.md` has been updated to point to the new, clearer documentation structure.

### Bug Fixes & Improvements
- ✅ **Dynamic Configuration Loading**: Removed hardcoded connection ID checks. The library now dynamically loads whatever connections are defined in `mysql.json`, eliminating confusing log messages.

---

## Version 2.1.0 - Advanced ORM Features & Sync Service

### Overview
This version introduces advanced ORM features like eager loading for all relationship types, high-performance bulk operations, and a completely overhauled, highly configurable schema synchronization service.

### New Features

#### Querying & Data Manipulation
- ✅ **`Include()` for Eager Loading**: The `QueryBuilder` now supports a `.Include()` method to eager load related entities, eliminating the "N+1" problem.
- ✅ **Full Relationship Support**: `Include()` works for one-to-many, many-to-one, and now many-to-many relationships.
- ✅ **`[ManyToMany]` Attribute**: A new attribute to define many-to-many relationships via a junction entity.
- ✅ **Bulk Insert**: New `InsertManyAsync()` method on the generic repository for high-performance batch inserts.
- ✅ **Bulk Update**: New `UpdateAsync()` method on the `QueryBuilder` allows updating multiple rows matching a `WHERE` clause in a single database call.

#### Schema Synchronization (`TableSyncService`)
- ✅ **`SyncMode` Configuration**: Replaced `EnableAutoSync` with a more powerful `SyncMode` enum (`None`, `Safe`, `Reconstruct`, `Destructive`) for fine-grained control over schema changes.
- ✅ **`Reconstruct` Mode**: New development-focused mode that can automatically drop and recreate foreign keys to apply schema changes that would otherwise be blocked.
- ✅ **`Destructive` Mode**: New development-focused mode that can `DROP` and recreate a table to force alignment with the model, wiping all data.
- ✅ **`SyncOnStartup`**: New configuration option to automatically run schema synchronization when the application starts.
- ✅ **`NamespacesToSync`**: New configuration to specify which namespaces to scan for models during a startup sync.

#### Documentation
- ✅ **Consolidated Documentation**: Removed several outdated markdown files and created a single, comprehensive `Documentation.md` file.
- ✅ **Updated README**: The `README.md` has been updated to be a concise entry point that links to the new documentation.
- ✅ **Updated Changelog**: This changelog has been updated with all the latest features.

---

## Version 2.0.0 - Complete Framework Rewrite

### Overview
Complete rewrite of CL.MySQL2 for CodeLogic 2.0 framework with full integration of modern patterns and comprehensive feature set.

### New Features

#### Core Architecture
- ✅ **Full CodeLogic Integration**: Implements `ILibrary` interface with complete lifecycle management
- ✅ **Dependency Injection**: Constructor-based DI for all services
- ✅ **Modern Logging**: Integrated with `CL.Core.ILogger` for comprehensive operation tracking
- ✅ **Configuration System**: Uses CodeLogic's `ConfigurationManager` for settings
- ✅ **Health Checks**: Automatic connection testing with detailed health status

#### Connection Management
- ✅ **ConnectionManager Service**: Optimized connection pooling and lifecycle management
- ✅ **Multiple Databases**: Support for named database connections
- ✅ **Connection Testing**: Automatic health checks on initialization
- ✅ **Server Info**: Retrieve MySQL server version and details
- ✅ **Transaction Support**: Automatic transaction management with rollback
- ✅ **Connection Caching**: Cached connection strings for performance

#### Repository Pattern
- ✅ **Generic Repository<T>**: Type-safe CRUD operations
- ✅ **Async/Await**: Fully asynchronous operations throughout
- ✅ **Query Caching**: Configurable cache TTL for read operations
- ✅ **Bulk Operations**: Batch insert support
- ✅ **Auto-mapping**: Automatic entity to/from database mapping
- ✅ **Error Handling**: Comprehensive exception handling with logging

#### QueryBuilder (NEW!)
- ✅ **Fluent API**: Readable, chainable query construction
- ✅ **Type Safety**: Compile-time checking for queries
- ✅ **WHERE Clauses**: Equals, GreaterThan, LessThan, Like, In, Between
- ✅ **Joins**: Inner, Left, Right, Cross joins
- ✅ **Aggregations**: Count, Sum, Avg, Min, Max
- ✅ **Grouping**: GROUP BY support with aggregates
- ✅ **Ordering**: ORDER BY with multiple columns
- ✅ **Pagination**: Built-in paged result support with metadata
- ✅ **Limiting**: Limit/Offset and Skip/Take methods
- ✅ **SQL Preview**: ToSql() method for debugging
- ✅ **Slow Query Logging**: Automatic detection and logging

#### Type System
- ✅ **Comprehensive Types**: Support for all MySQL data types
- ✅ **Type Conversion**: Bidirectional C# ↔ MySQL conversion
- ✅ **Modern C# Types**: DateOnly, TimeOnly, Guid support
- ✅ **JSON Support**: Native JSON column type handling
- ✅ **Enum Support**: Automatic enum to integer mapping

#### Model Attributes
- ✅ **[Table]**: Define table name, engine, charset, collation
- ✅ **[Column]**: Define data type, size, constraints, indexes
- ✅ **[ForeignKey]**: Define relationships with cascade options
- ✅ **[CompositeIndex]**: Multi-column index support
- ✅ **[Ignore]**: Exclude properties from database operations

### Migration from 1.x

#### Breaking Changes
1. **Namespace Change**: `CL.MySQL2` remains but internal structure changed
2. **Initialization**: Now uses `ILibrary` pattern with CodeLogic lifecycle
3. **Logging**: Replaces old `CLF_Logging` with `ILogger` interface
4. **Configuration**: Uses CodeLogic config system instead of custom solution
5. **Static Classes**: Removed static `MySQL2` class, use library instance

#### Migration Guide

**Old (1.x):**
```csharp
// Static access
var repo = MySQL2.Repository.Create<User>();
var query = MySQL2.Query.For<User>();
```

**New (2.0):**
```csharp
// Get library instance
var mysql2 = libraryManager.GetLibrary<MySQL2Library>();

// Create services
var repo = mysql2.GetRepository<User>();
var query = mysql2.GetQueryBuilder<User>();
```

### Configuration Changes

**Old Configuration:**
Required manual setup via `LibraryConfiguration.GetConfigClassAsync`

**New Configuration:**
Automatic loading from `config/mysql.json`:

```json
{
  "host": "localhost",
  "port": 3306,
  "database": "mydb",
  "username": "user",
  "password": "pass",
  "min_pool_size": 5,
  "max_pool_size": 100,
  "enable_logging": true,
  "enable_caching": true
}
```

### Performance Improvements
- Connection string caching reduces overhead
- Property reflection caching for entity mapping
- Optimized type conversion with minimal allocations
- Query result caching with configurable TTL
- Connection pooling with configurable limits

### Developer Experience
- Comprehensive XML documentation
- Detailed README with examples
- EXAMPLES.md with real-world scenarios
- IntelliSense support throughout
- SQL debugging with ToSql()
- Clear error messages and logging

### Files Created

```
src/Libraries/CL.MySQL2/
├── Models/
│   ├── Attributes.cs          (Table, Column, ForeignKey attributes)
│   ├── Configuration.cs       (DatabaseConfiguration, SslMode)
│   ├── DataTypes.cs          (All MySQL data types)
│   └── QueryModels.cs        (OperationResult, PagedResult, query models)
├── Core/
│   └── TypeConverter.cs      (Type conversion utilities)
├── Services/
│   ├── ConnectionManager.cs  (Connection pooling and management)
│   ├── Repository.cs         (Generic CRUD repository)
│   └── QueryBuilder.cs       (Fluent query builder) ★ NEW
├── MySQL2Library.cs          (Main library implementation)
├── CL.MySQL2.csproj         (Project file)
├── README.md                (Usage documentation)
├── EXAMPLES.md              (Comprehensive examples) ★ NEW
└── CHANGELOG.md             (This file)
```

### Testing
- ✅ Compiles without errors (CL.MySQL2 specific)
- ✅ Framework integration verified
- ✅ Type safety confirmed
- ⏳ Unit tests pending
- ⏳ Integration tests pending

### Known Issues
- Parent CodeLogic framework has build errors (not related to CL.MySQL2)
- Requires CodeLogic framework fixes for full build

### Dependencies
- **MySqlConnector**: 2.4.0 (MySQL database driver)
- **CL.Core**: 2.0.0+ (CodeLogic core abstractions)
- **CodeLogic**: 2.0.0+ (Main framework)

### Next Steps
1. Fix CodeLogic framework build errors
2. Add unit tests for all services
3. Add integration tests with test database
4. Performance benchmarking
5. Add table synchronization/migration support
6. Add query execution statistics

### Credits
Rebuilt from ground up for CodeLogic 2.0 by Media2A.

---

## Previous Versions

### Version 1.x (Legacy)
- Basic MySQL support
- Repository pattern
- Query builder
- Model sync
- Static class architecture