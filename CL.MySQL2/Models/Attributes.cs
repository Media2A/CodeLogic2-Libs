namespace CL.MySQL2.Models;

/// <summary>
/// Attribute for defining MySQL table properties on a model class.
/// Apply this attribute to classes that represent database tables.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the table in the database.
    /// If not specified, the class name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the storage engine used by the table.
    /// Default is InnoDB.
    /// </summary>
    public TableEngine Engine { get; set; } = TableEngine.InnoDB;

    /// <summary>
    /// Gets or sets the character set for the table.
    /// Default is Utf8mb4.
    /// </summary>
    public Charset Charset { get; set; } = Charset.Utf8mb4;

    /// <summary>
    /// Gets or sets the collation for the table.
    /// If not specified, the default collation for the charset will be used.
    /// </summary>
    public string? Collation { get; set; }

    /// <summary>
    /// Gets or sets a comment describing the table.
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Attribute for defining MySQL column properties on a model property.
/// Apply this attribute to properties that represent database columns.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the column in the database.
    /// If not specified, the property name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the MySQL data type for this column.
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// Gets or sets the size/length of the column (for VARCHAR, CHAR, etc.).
    /// For VARCHAR, default is 255 if not specified.
    /// </summary>
    public int Size { get; set; } = 0;

    /// <summary>
    /// Gets or sets the precision for DECIMAL type (total number of digits).
    /// </summary>
    public int Precision { get; set; } = 10;

    /// <summary>
    /// Gets or sets the scale for DECIMAL type (number of digits after decimal point).
    /// </summary>
    public int Scale { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether this column is the primary key.
    /// </summary>
    public bool Primary { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this column should auto-increment.
    /// Only valid for integer types.
    /// </summary>
    public bool AutoIncrement { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this column cannot contain null values.
    /// </summary>
    public bool NotNull { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this column must have unique values.
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Gets or sets whether an index should be created for this column.
    /// </summary>
    public bool Index { get; set; } = false;

    /// <summary>
    /// Gets or sets the default value for this column.
    /// Use special values like "CURRENT_TIMESTAMP", "NULL", etc.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the character set for this column (overrides table charset).
    /// </summary>
    public Charset? Charset { get; set; }

    /// <summary>
    /// Gets or sets a comment describing the column.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets whether this column should be unsigned (for numeric types).
    /// </summary>
    public bool Unsigned { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this column should automatically update on row modification.
    /// Only valid for TIMESTAMP columns.
    /// </summary>
    public bool OnUpdateCurrentTimestamp { get; set; } = false;
}

/// <summary>
/// Attribute for defining a foreign key constraint on a column.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ForeignKeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the table that this foreign key references.
    /// </summary>
    public required string ReferenceTable { get; set; }

    /// <summary>
    /// Gets or sets the column in the reference table.
    /// </summary>
    public required string ReferenceColumn { get; set; }

    /// <summary>
    /// Gets or sets the action to take when the referenced row is deleted.
    /// Default is Restrict.
    /// </summary>
    public ForeignKeyAction OnDelete { get; set; } = ForeignKeyAction.Restrict;

    /// <summary>
    /// Gets or sets the action to take when the referenced row is updated.
    /// Default is Restrict.
    /// </summary>
    public ForeignKeyAction OnUpdate { get; set; } = ForeignKeyAction.Restrict;

    /// <summary>
    /// Gets or sets the name of the foreign key constraint.
    /// If not specified, MySQL will auto-generate a name.
    /// </summary>
    public string? ConstraintName { get; set; }
}

/// <summary>
/// Enumerates the actions that can be taken on foreign key constraints.
/// </summary>
public enum ForeignKeyAction
{
    /// <summary>
    /// Restrict the operation (default).
    /// </summary>
    Restrict,

    /// <summary>
    /// Cascade the operation to related rows.
    /// </summary>
    Cascade,

    /// <summary>
    /// Set the foreign key column to NULL.
    /// </summary>
    SetNull,

    /// <summary>
    /// Prevent the operation (same as Restrict in MySQL).
    /// </summary>
    NoAction,

    /// <summary>
    /// Set the foreign key column to its default value.
    /// </summary>
    SetDefault
}

/// <summary>
/// Attribute to mark a property that should be ignored during database operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IgnoreAttribute : Attribute
{
}

/// <summary>
/// Attribute for creating a composite index on multiple columns.
/// Apply this attribute multiple times to a class to define different composite indexes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CompositeIndexAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the composite index.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// Gets the column names that make up this composite index.
    /// </summary>
    public string[] ColumnNames { get; }

    /// <summary>
    /// Gets or sets whether this is a unique composite index.
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeIndexAttribute"/> class.
    /// </summary>
    /// <param name="indexName">The name of the composite index.</param>
    /// <param name="columnNames">The column names that make up this composite index.</param>
    public CompositeIndexAttribute(string indexName, params string[] columnNames)
    {
        IndexName = indexName;
        ColumnNames = columnNames;
    }
}

/// <summary>
/// Attribute for defining a many-to-many relationship on a navigation property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ManyToManyAttribute : Attribute
{
    /// <summary>
    /// Gets the entity type of the junction table that links the two entities.
    /// </summary>
    public Type JunctionEntityType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManyToManyAttribute"/> class.
    /// </summary>
    /// <param name="junctionEntityType">The model type representing the junction table.</param>
    public ManyToManyAttribute(Type junctionEntityType)
    {
        JunctionEntityType = junctionEntityType;
    }
}
