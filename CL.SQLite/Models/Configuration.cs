namespace CL.SQLite.Models;

/// <summary>
/// Configuration settings for SQLite database connections
/// </summary>
public class SQLiteConfiguration
{
    /// <summary>
    /// Gets or sets the path to the SQLite database file
    /// </summary>
    public string DatabasePath { get; set; } = "database.db";

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public uint ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds
    /// </summary>
    public uint CommandTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets whether to skip automatic table synchronization on startup
    /// </summary>
    public bool SkipTableSync { get; set; } = false;

    /// <summary>
    /// Gets or sets the cache mode for the database connection
    /// </summary>
    public CacheMode CacheMode { get; set; } = CacheMode.Default;

    /// <summary>
    /// Gets or sets whether to use Write-Ahead Logging (WAL) mode
    /// </summary>
    public bool UseWAL { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable foreign key constraints
    /// </summary>
    public bool EnableForeignKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of connections in the pool
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;
}

/// <summary>
/// SQLite cache modes
/// </summary>
public enum CacheMode
{
    /// <summary>
    /// Default cache mode
    /// </summary>
    Default,

    /// <summary>
    /// Private cache mode - each connection has its own cache
    /// </summary>
    Private,

    /// <summary>
    /// Shared cache mode - connections share a single cache
    /// </summary>
    Shared
}
