# 1. Getting Started

Getting started with CL.MySQL2 involves setting up a configuration file that tells the library how to connect to your database(s). The library is designed to be flexible, supporting multiple database connections from a single application.

## Configuration (`mysql.json`)

Configuration is managed via a `mysql.json` file located in your project's central `config` directory. If this file is not found on startup, the library will automatically generate a template for you with default settings.

A typical configuration file supports multiple connection profiles (e.g., `Default`, `Demo`, `Analytics`).

### Example `mysql.json`

```json
{
  "Default": {
    "enabled": true,
    "host": "localhost",
    "port": 3306,
    "database": "main_database",
    "username": "root",
    "password": "",
    "min_pool_size": 5,
    "max_pool_size": 100,
    "backup_on_sync": true,
    "sync_mode": "Safe",
    "sync_on_startup": false,
    "namespaces_to_sync": [ "Your.Model.Namespace" ] 
  },
  "Analytics": {
    "enabled": true,
    "host": "analytics-db.example.com",
    "port": 3306,
    "database": "analytics_db",
    "username": "readonly_user",
    "password": "secure_password",
    "sync_mode": "None" 
  }
}
```

### Key Configuration Properties

- `enabled`: If `false`, this connection profile will be ignored.
- `host`, `port`, `database`, `username`, `password`: Standard database connection details.
- `min_pool_size` / `max_pool_size`: Configures the built-in connection pooling for performance.
- `sync_mode`: Defines the strategy for the schema synchronization service. (See the [Schema Synchronization](./6-schema-synchronization.md) guide for details).
- `sync_on_startup`: If `true`, the library will automatically run the schema synchronizer when the application starts.
- `namespaces_to_sync`: A list of C# namespaces to scan for models when `sync_on_startup` is enabled.
