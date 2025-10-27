using MySqlConnector;
using System.Text;

namespace CL.MySQL2.Models;

/// <summary>
/// Configuration settings for a MySQL database connection.
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this connection configuration.
    /// </summary>
    public string ConnectionId { get; set; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether this connection configuration is enabled.
    /// When false, this connection will not be initialized or used.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the database server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database server port.
    /// Default is 3306 for MySQL.
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Gets or sets the database name/schema.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for database authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for database authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum pool size for connection pooling.
    /// Default is 5.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum pool size for connection pooling.
    /// Default is 100.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum idle time for a pooled connection in seconds.
    /// Connections idle longer than this will be closed.
    /// Default is 60 seconds.
    /// </summary>
    public int MaxIdleTime { get; set; } = 60;

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    public SslMode SslMode { get; set; } = SslMode.Preferred;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic table synchronization.
    /// When true, tables will be automatically created/updated to match model definitions.
    /// Default is true.
    /// </summary>
    public bool EnableAutoSync { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable query result caching.
    /// Default is true.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache time-to-live in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int DefaultCacheTtl { get; set; } = 300;

    /// <summary>
    /// Gets or sets the character set for the connection.
    /// Default is utf8mb4.
    /// </summary>
    public string CharacterSet { get; set; } = "utf8mb4";

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging.
    /// Default is false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to log slow queries.
    /// Default is true.
    /// </summary>
    public bool LogSlowQueries { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold in milliseconds for logging slow queries.
    /// Queries taking longer than this will be logged.
    /// Default is 1000 milliseconds (1 second).
    /// </summary>
    public int SlowQueryThreshold { get; set; } = 1000;

    /// <summary>
    /// Builds a MySQL connection string from the configuration.
    /// </summary>
    public string BuildConnectionString()
    {
        var builder = new StringBuilder();
        builder.Append($"Server={Host};");
        builder.Append($"Port={Port};");
        builder.Append($"Database={Database};");
        builder.Append($"User={Username};");
        builder.Append($"Password={Password};");
        builder.Append($"ConnectionTimeout={ConnectionTimeout};");
        builder.Append($"DefaultCommandTimeout={CommandTimeout};");
        builder.Append($"MinimumPoolSize={MinPoolSize};");
        builder.Append($"MaximumPoolSize={MaxPoolSize};");
        builder.Append($"ConnectionIdleTimeout={MaxIdleTime};");
        builder.Append($"SslMode={SslMode};");
        builder.Append($"CharSet={CharacterSet};");
        builder.Append("Pooling=true;");
        builder.Append("AllowUserVariables=true;");

        return builder.ToString();
    }
}

/// <summary>
/// Enumerates the SSL modes for database connections.
/// </summary>
public enum SslMode
{
    /// <summary>
    /// No SSL connection.
    /// </summary>
    None,

    /// <summary>
    /// Prefer SSL if available, otherwise use non-SSL.
    /// </summary>
    Preferred,

    /// <summary>
    /// Require SSL connection.
    /// </summary>
    Required,

    /// <summary>
    /// Require SSL with certificate verification.
    /// </summary>
    VerifyCA,

    /// <summary>
    /// Require SSL with full certificate verification.
    /// </summary>
    VerifyFull
}
