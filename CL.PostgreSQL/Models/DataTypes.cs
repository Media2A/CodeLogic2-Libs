namespace CL.PostgreSQL.Models;

/// <summary>
/// Enumerates the supported PostgreSQL data types for model properties.
/// </summary>
public enum DataType
{
    /// <summary>
    /// A very small integer. Range: -32,768 to 32,767.
    /// </summary>
    SmallInt,

    /// <summary>
    /// A standard integer. Range: -2,147,483,648 to 2,147,483,647.
    /// </summary>
    Int,

    /// <summary>
    /// A large integer. Range: -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807.
    /// </summary>
    BigInt,

    /// <summary>
    /// A small (single-precision) floating-point number.
    /// </summary>
    Real,

    /// <summary>
    /// A normal-size (double-precision) floating-point number.
    /// </summary>
    DoublePrecision,

    /// <summary>
    /// A fixed-point decimal number for precise calculations (e.g., money).
    /// </summary>
    Numeric,

    /// <summary>
    /// A date and time combination.
    /// </summary>
    Timestamp,

    /// <summary>
    /// A date value.
    /// </summary>
    Date,

    /// <summary>
    /// A time value.
    /// </summary>
    Time,

    /// <summary>
    /// A time value with timezone.
    /// </summary>
    TimeTz,

    /// <summary>
    /// A timestamp with timezone.
    /// </summary>
    TimestampTz,

    /// <summary>
    /// A fixed-length string (up to 255 characters).
    /// </summary>
    Char,

    /// <summary>
    /// A variable-length string (up to 65,535 characters).
    /// </summary>
    VarChar,

    /// <summary>
    /// A variable-length text string (unlimited).
    /// </summary>
    Text,

    /// <summary>
    /// A JSON-encoded text string with automatic validation.
    /// </summary>
    Json,

    /// <summary>
    /// A JSONB-encoded binary JSON with indexing support.
    /// </summary>
    Jsonb,

    /// <summary>
    /// A universally unique identifier (UUID/GUID).
    /// </summary>
    Uuid,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Bool,

    /// <summary>
    /// A binary large object.
    /// </summary>
    Bytea,

    /// <summary>
    /// An array of integers.
    /// </summary>
    IntArray,

    /// <summary>
    /// An array of big integers.
    /// </summary>
    BigIntArray,

    /// <summary>
    /// An array of text values.
    /// </summary>
    TextArray,

    /// <summary>
    /// An array of numeric values.
    /// </summary>
    NumericArray
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
