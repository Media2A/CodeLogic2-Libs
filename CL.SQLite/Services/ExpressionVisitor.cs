using CL.SQLite.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace CL.SQLite.Services;

/// <summary>
/// Converts LINQ Expression Trees to SQLite WHERE clause predicates.
/// Handles common LINQ patterns and translates them to SQL.
/// </summary>
public class ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
{
    private readonly List<WhereCondition> _conditions = new();
    private int _parameterIndex = 0;
    private string _currentLogicalOperator = "AND";

    /// <summary>
    /// Converts a LINQ expression to WHERE conditions.
    /// </summary>
    public static List<WhereCondition> Parse<T>(Expression<Func<T, bool>> expression)
    {
        var visitor = new ExpressionVisitor();
        visitor.Visit(expression.Body);
        return visitor._conditions;
    }

    /// <summary>
    /// Converts a LINQ expression to an ORDER BY column and direction.
    /// </summary>
    public static (string Column, SortOrder Order) ParseOrderBy<T>(Expression<Func<T, object?>> expression)
    {
        var member = GetMemberExpression(expression.Body);
        var columnName = member.Member.Name;
        return (columnName, SortOrder.Asc);
    }

    /// <summary>
    /// Converts a LINQ expression to an ORDER BY column with direction.
    /// </summary>
    public static (string Column, SortOrder Order) ParseOrderBy<T, TKey>(Expression<Func<T, TKey>> expression, bool descending = false)
    {
        var member = GetMemberExpression(expression.Body);
        var columnName = member.Member.Name;
        return (columnName, descending ? SortOrder.Desc : SortOrder.Asc);
    }

    /// <summary>
    /// Extracts column names from a projection expression.
    /// </summary>
    public static List<string> ParseSelect<T>(Expression<Func<T, object?>> expression)
    {
        var columns = new List<string>();

        if (expression.Body is NewExpression newExpr)
        {
            foreach (var arg in newExpr.Arguments)
            {
                var member = GetMemberExpression(arg);
                if (member != null)
                    columns.Add(member.Member.Name);
            }
        }
        else if (expression.Body is MemberExpression member)
        {
            columns.Add(member.Member.Name);
        }

        return columns;
    }

    /// <summary>
    /// Extracts column names from a GROUP BY expression.
    /// </summary>
    public static List<string> ParseGroupBy<T>(Expression<Func<T, object?>> expression)
    {
        var columns = new List<string>();

        if (expression.Body is NewExpression newExpr)
        {
            foreach (var arg in newExpr.Arguments)
            {
                var member = GetMemberExpression(arg);
                if (member != null)
                    columns.Add(member.Member.Name);
            }
        }
        else if (expression.Body is MemberExpression member)
        {
            columns.Add(member.Member.Name);
        }

        return columns;
    }

    /// <summary>
    /// Extracts column names from a GROUP BY expression (generic version).
    /// </summary>
    public static List<string> ParseGroupBy<T, TKey>(Expression<Func<T, TKey>> expression)
    {
        var columns = new List<string>();
        var member = GetMemberExpression(expression.Body);
        if (member != null)
            columns.Add(member.Member.Name);
        return columns;
    }

    public override Expression Visit(Expression? node)
    {
        if (node == null)
            return node;

        return node.NodeType switch
        {
            ExpressionType.AndAlso => VisitAndAlso((BinaryExpression)node),
            ExpressionType.OrElse => VisitOrElse((BinaryExpression)node),
            ExpressionType.Equal => VisitBinary((BinaryExpression)node, "="),
            ExpressionType.NotEqual => VisitBinary((BinaryExpression)node, "!="),
            ExpressionType.GreaterThan => VisitBinary((BinaryExpression)node, ">"),
            ExpressionType.GreaterThanOrEqual => VisitBinary((BinaryExpression)node, ">="),
            ExpressionType.LessThan => VisitBinary((BinaryExpression)node, "<"),
            ExpressionType.LessThanOrEqual => VisitBinary((BinaryExpression)node, "<="),
            ExpressionType.Call => VisitMethodCall((MethodCallExpression)node),
            ExpressionType.Not => VisitNot((UnaryExpression)node),
            _ => base.Visit(node)
        };
    }

    private Expression VisitAndAlso(BinaryExpression node)
    {
        var previousOperator = _currentLogicalOperator;
        _currentLogicalOperator = "AND";

        Visit(node.Left);
        Visit(node.Right);

        _currentLogicalOperator = previousOperator;
        return node;
    }

    private Expression VisitOrElse(BinaryExpression node)
    {
        var previousOperator = _currentLogicalOperator;
        _currentLogicalOperator = "OR";

        Visit(node.Left);
        Visit(node.Right);

        _currentLogicalOperator = previousOperator;
        return node;
    }

    private Expression VisitBinary(BinaryExpression node, string operatorSymbol)
    {
        var member = GetMemberExpression(node.Left) ?? GetMemberExpression(node.Right);

        if (member == null)
            return node;

        var columnName = member.Member.Name;
        var value = GetValue(node.Right);

        if (value == null && node.Left.Type.IsValueType == false)
        {
            // Handle null comparisons
            var op = operatorSymbol == "=" ? "IS" : "IS NOT";
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = op,
                Value = null,
                LogicalOperator = _currentLogicalOperator
            });
        }
        else
        {
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = operatorSymbol,
                Value = value,
                LogicalOperator = _currentLogicalOperator
            });
        }

        return node;
    }

    private Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        var member = GetMemberExpression(node.Object) ?? GetMemberExpression(node.Arguments.FirstOrDefault());

        if (member == null)
            return node;

        var columnName = member.Member.Name;

        // String methods
        if (method.Name == "Contains" && node.Object?.Type == typeof(string))
        {
            var value = GetValue(node.Arguments[0]);
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = "LIKE",
                Value = value,
                LogicalOperator = _currentLogicalOperator
            });
        }
        else if (method.Name == "StartsWith" && node.Object?.Type == typeof(string))
        {
            var value = GetValue(node.Arguments[0]);
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = "LIKE",
                Value = $"{value}%",
                LogicalOperator = _currentLogicalOperator
            });
        }
        else if (method.Name == "EndsWith" && node.Object?.Type == typeof(string))
        {
            var value = GetValue(node.Arguments[0]);
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = "LIKE",
                Value = $"%{value}",
                LogicalOperator = _currentLogicalOperator
            });
        }
        // Collection methods (Contains for arrays/lists)
        else if (method.Name == "Contains" && (node.Object?.Type.IsGenericType == true || node.Object?.Type.IsArray == true))
        {
            var values = GetValue(node.Object);
            if (values is System.Collections.IEnumerable enumerable)
            {
                var valueList = enumerable.Cast<object>().ToArray();
                _conditions.Add(new WhereCondition
                {
                    Column = columnName,
                    Operator = "IN",
                    Value = valueList,
                    LogicalOperator = _currentLogicalOperator
                });
            }
        }

        return node;
    }

    private Expression VisitNot(UnaryExpression node)
    {
        // Handle NOT expressions like !u.IsActive
        if (node.Operand is MemberExpression member && member.Type == typeof(bool))
        {
            var columnName = member.Member.Name;
            _conditions.Add(new WhereCondition
            {
                Column = columnName,
                Operator = "=",
                Value = false,
                LogicalOperator = _currentLogicalOperator
            });
        }

        return node;
    }

    private static MemberExpression? GetMemberExpression(Expression? expression)
    {
        if (expression is MemberExpression member)
            return member;

        if (expression is UnaryExpression unary)
            return GetMemberExpression(unary.Operand);

        return null;
    }

    private static object? GetValue(Expression? expression)
    {
        if (expression == null)
            return null;

        if (expression is ConstantExpression constant)
            return constant.Value;

        if (expression is MemberExpression member)
        {
            var getMethod = ((PropertyInfo)member.Member).GetGetMethod();
            if (getMethod != null)
            {
                var instance = GetValue(member.Expression);
                return getMethod.Invoke(instance, null);
            }
        }

        // Try to compile and evaluate the expression
        try
        {
            var compiled = Expression.Lambda<Func<object?>>(
                Expression.Convert(expression, typeof(object))
            ).Compile();

            return compiled();
        }
        catch
        {
            return null;
        }
    }
}
