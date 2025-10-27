namespace CL.SQLite.Models;

/// <summary>
/// Supported SQLite data types using type affinity
/// </summary>
public enum SQLiteDataType
{
    /// <summary>
    /// Integer value - stored as 1, 2, 3, 4, 6, or 8 bytes depending on magnitude
    /// </summary>
    INTEGER,

    /// <summary>
    /// Floating point value - stored as an 8-byte IEEE floating point number
    /// </summary>
    REAL,

    /// <summary>
    /// Text string - stored using the database encoding (UTF-8, UTF-16BE or UTF-16LE)
    /// </summary>
    TEXT,

    /// <summary>
    /// Binary large object - stored exactly as it was input
    /// </summary>
    BLOB,

    /// <summary>
    /// Numeric value - can be stored as INTEGER or REAL depending on the value
    /// </summary>
    NUMERIC,

    /// <summary>
    /// Date and time value - stored as TEXT in ISO 8601 format ('YYYY-MM-DD HH:MM:SS')
    /// </summary>
    DATETIME,

    /// <summary>
    /// Date value - stored as TEXT in ISO 8601 format ('YYYY-MM-DD')
    /// </summary>
    DATE,

    /// <summary>
    /// Boolean value - stored as INTEGER (0 for false, 1 for true)
    /// </summary>
    BOOLEAN,

    /// <summary>
    /// Universally unique identifier - stored as TEXT or BLOB
    /// </summary>
    UUID
}
