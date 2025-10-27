namespace CL.MySQL2.Models;

/// <summary>
/// Enumerates the supported MySQL data types for model properties.
/// </summary>
public enum DataType
{
    /// <summary>
    /// A very small integer. Range: -128 to 127 or 0 to 255 (unsigned).
    /// </summary>
    TinyInt,

    /// <summary>
    /// A small integer. Range: -32,768 to 32,767 or 0 to 65,535 (unsigned).
    /// </summary>
    SmallInt,

    /// <summary>
    /// A medium integer. Range: -8,388,608 to 8,388,607 or 0 to 16,777,215 (unsigned).
    /// </summary>
    MediumInt,

    /// <summary>
    /// A standard integer. Range: -2,147,483,648 to 2,147,483,647 or 0 to 4,294,967,295 (unsigned).
    /// </summary>
    Int,

    /// <summary>
    /// A large integer. Range: -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807.
    /// </summary>
    BigInt,

    /// <summary>
    /// A small (single-precision) floating-point number.
    /// </summary>
    Float,

    /// <summary>
    /// A normal-size (double-precision) floating-point number.
    /// </summary>
    Double,

    /// <summary>
    /// A fixed-point decimal number for precise calculations (e.g., money).
    /// </summary>
    Decimal,

    /// <summary>
    /// A date and time combination, formatted as 'YYYY-MM-DD HH:MM:SS'.
    /// </summary>
    DateTime,

    /// <summary>
    /// A date value, formatted as 'YYYY-MM-DD'.
    /// </summary>
    Date,

    /// <summary>
    /// A time value, formatted as 'HH:MM:SS'.
    /// </summary>
    Time,

    /// <summary>
    /// A timestamp, automatically updated on row modification.
    /// </summary>
    Timestamp,

    /// <summary>
    /// A year value in 4-digit format.
    /// </summary>
    Year,

    /// <summary>
    /// A fixed-length string (up to 255 characters).
    /// </summary>
    Char,

    /// <summary>
    /// A variable-length string (up to 65,535 characters).
    /// </summary>
    VarChar,

    /// <summary>
    /// A text data type with a maximum length of 255 characters.
    /// </summary>
    TinyText,

    /// <summary>
    /// A text data type with a maximum length of 65,535 characters.
    /// </summary>
    Text,

    /// <summary>
    /// A medium-sized text data type with a maximum length of 16 MB.
    /// </summary>
    MediumText,

    /// <summary>
    /// A large text data type with a maximum length of 4 GB.
    /// </summary>
    LongText,

    /// <summary>
    /// A JSON-encoded text string with automatic validation and indexing.
    /// </summary>
    Json,

    /// <summary>
    /// A fixed-length binary string.
    /// </summary>
    Binary,

    /// <summary>
    /// A variable-length binary string.
    /// </summary>
    VarBinary,

    /// <summary>
    /// A binary large object with a maximum length of 255 bytes.
    /// </summary>
    TinyBlob,

    /// <summary>
    /// A binary large object with a maximum length of 65,535 bytes.
    /// </summary>
    Blob,

    /// <summary>
    /// A medium-sized binary large object with a maximum length of 16 MB.
    /// </summary>
    MediumBlob,

    /// <summary>
    /// A binary large object with a maximum length of 4 GB.
    /// </summary>
    LongBlob,

    /// <summary>
    /// A universally unique identifier (UUID/GUID).
    /// </summary>
    Uuid,

    /// <summary>
    /// An enumeration of string values.
    /// </summary>
    Enum,

    /// <summary>
    /// A set of string values (multiple values can be selected).
    /// </summary>
    Set,

    /// <summary>
    /// A boolean value (stored as TINYINT(1)).
    /// </summary>
    Bool
}

/// <summary>
/// Enumerates the supported MySQL table storage engines.
/// </summary>
public enum TableEngine
{
    /// <summary>
    /// InnoDB storage engine (default). Supports transactions, foreign keys, and row-level locking.
    /// </summary>
    InnoDB,

    /// <summary>
    /// MyISAM storage engine. Fast for read-heavy operations but lacks transaction support.
    /// </summary>
    MyISAM,

    /// <summary>
    /// Memory storage engine. Stores data in RAM for extremely fast access.
    /// </summary>
    Memory,

    /// <summary>
    /// Archive storage engine. Optimized for storing and retrieving large amounts of archived data.
    /// </summary>
    Archive,

    /// <summary>
    /// CSV storage engine. Stores data in comma-separated values format.
    /// </summary>
    CSV
}

/// <summary>
/// Enumerates the supported character sets for columns and tables.
/// </summary>
public enum Charset
{
    /// <summary>
    /// UTF-8 encoding (3-byte maximum per character).
    /// </summary>
    Utf8,

    /// <summary>
    /// UTF-8 encoding (4-byte maximum per character, supports emojis and special characters).
    /// </summary>
    Utf8mb4,

    /// <summary>
    /// Latin1 (Western European) character set.
    /// </summary>
    Latin1,

    /// <summary>
    /// ASCII character set.
    /// </summary>
    Ascii,

    /// <summary>
    /// Binary character set (no character encoding).
    /// </summary>
    Binary
}

/// <summary>
/// Enumerates the sort order for ORDER BY clauses.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Sort in ascending order (A-Z, 0-9).
    /// </summary>
    Asc,

    /// <summary>
    /// Sort in descending order (Z-A, 9-0).
    /// </summary>
    Desc
}

/// <summary>
/// Enumerates the types of database operations.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Create operation (INSERT).
    /// </summary>
    Create,

    /// <summary>
    /// Read operation (SELECT).
    /// </summary>
    Read,

    /// <summary>
    /// Update operation (UPDATE).
    /// </summary>
    Update,

    /// <summary>
    /// Delete operation (DELETE).
    /// </summary>
    Delete
}
