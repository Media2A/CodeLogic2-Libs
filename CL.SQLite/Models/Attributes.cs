namespace CL.SQLite.Models;

/// <summary>
/// Specifies that a class represents a SQLite database table
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SQLiteTableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the table in the database
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// Initializes a new instance of the SQLiteTableAttribute
    /// </summary>
    /// <param name="tableName">The name of the table</param>
    public SQLiteTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}

/// <summary>
/// Specifies properties for a SQLite database column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SQLiteColumnAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether the column is a primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets whether the column should be indexed
    /// </summary>
    public bool IsIndexed { get; set; }

    /// <summary>
    /// Gets or sets whether the column must have unique values
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets whether the column value is auto-incremented
    /// </summary>
    public bool IsAutoIncrement { get; set; }

    /// <summary>
    /// Gets or sets the name of the column in the database
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the data type of the column
    /// </summary>
    public SQLiteDataType DataType { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the column (if applicable)
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets whether the column cannot contain null values
    /// </summary>
    public bool IsNotNull { get; set; }

    /// <summary>
    /// Gets or sets the default value of the column
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Specifies a foreign key constraint for a column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SQLiteForeignKeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the referenced table name
    /// </summary>
    public string ReferencedTable { get; set; }

    /// <summary>
    /// Gets or sets the referenced column name
    /// </summary>
    public string ReferencedColumn { get; set; }

    /// <summary>
    /// Gets or sets the action to take on delete
    /// </summary>
    public ForeignKeyAction OnDelete { get; set; } = ForeignKeyAction.NoAction;

    /// <summary>
    /// Gets or sets the action to take on update
    /// </summary>
    public ForeignKeyAction OnUpdate { get; set; } = ForeignKeyAction.NoAction;

    /// <summary>
    /// Initializes a new instance of the SQLiteForeignKeyAttribute
    /// </summary>
    /// <param name="referencedTable">The referenced table name</param>
    /// <param name="referencedColumn">The referenced column name</param>
    public SQLiteForeignKeyAttribute(string referencedTable, string referencedColumn)
    {
        ReferencedTable = referencedTable;
        ReferencedColumn = referencedColumn;
    }
}

/// <summary>
/// Foreign key actions for referential integrity
/// </summary>
public enum ForeignKeyAction
{
    /// <summary>
    /// No action is taken
    /// </summary>
    NoAction,

    /// <summary>
    /// Restrict the operation
    /// </summary>
    Restrict,

    /// <summary>
    /// Set the foreign key to NULL
    /// </summary>
    SetNull,

    /// <summary>
    /// Set the foreign key to its default value
    /// </summary>
    SetDefault,

    /// <summary>
    /// Cascade the operation to dependent rows
    /// </summary>
    Cascade
}
