using System.Linq.Expressions;
using System.Reflection;
using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Domain;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Translates the provider-agnostic <see cref="SortExpression"/>/<see cref="FilterNode"/> trees from a
/// paged request into server-side <see cref="IQueryable{T}"/> operations over <see cref="Issue"/>.
/// Field names are resolved against a small allow-list so callers cannot sort/filter on arbitrary members.
/// </summary>
internal static class IssueQueryTranslator
{
    private static readonly Dictionary<string, string> FieldMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Issue.Key)] = nameof(Issue.Key),
        [nameof(Issue.Title)] = nameof(Issue.Title),
        [nameof(Issue.Description)] = nameof(Issue.Description),
        [nameof(Issue.Status)] = nameof(Issue.Status),
        [nameof(Issue.Priority)] = nameof(Issue.Priority),
        [nameof(Issue.AssigneeId)] = nameof(Issue.AssigneeId),
        [nameof(Issue.CreatedUtc)] = nameof(Issue.CreatedUtc),
        [nameof(Issue.UpdatedUtc)] = nameof(Issue.UpdatedUtc),
    };

    /// <summary>Determines whether <paramref name="field"/> is an allowed sort/filter field.</summary>
    public static bool IsKnownField(string field) => !string.IsNullOrWhiteSpace(field) && FieldMap.ContainsKey(field);

    /// <summary>Applies the filter tree (if any) to the query.</summary>
    public static IQueryable<Issue> ApplyFilter(IQueryable<Issue> query, FilterGroup? filter)
    {
        if (filter is null || filter.Nodes.Count == 0) return query;

        var parameter = Expression.Parameter(typeof(Issue), "i");
        var body = BuildNode(filter, parameter);
        var predicate = Expression.Lambda<Func<Issue, bool>>(body, parameter);
        return query.Where(predicate);
    }

    /// <summary>Applies the ordered set of sort instructions to the query, defaulting to newest first.</summary>
    public static IQueryable<Issue> ApplySort(IQueryable<Issue> query, IReadOnlyList<SortExpression> sorts)
    {
        if (sorts.Count == 0) return query.OrderByDescending(i => i.CreatedUtc);

        IOrderedQueryable<Issue>? ordered = null;
        foreach (var sort in sorts)
        {
            ordered = ApplyOrder(ordered ?? query, sort, isFirst: ordered is null);
        }

        return ordered!;
    }

    private static IOrderedQueryable<Issue> ApplyOrder(IQueryable<Issue> query, SortExpression sort, bool isFirst)
    {
        var member = FieldMap[sort.Field];
        var parameter = Expression.Parameter(typeof(Issue), "i");
        var property = Expression.Property(parameter, member);
        var keySelector = Expression.Lambda(property, parameter);

        var methodName = (isFirst, sort.Direction) switch
        {
            (true, SortDirection.Descending) => nameof(Queryable.OrderByDescending),
            (true, _) => nameof(Queryable.OrderBy),
            (false, SortDirection.Descending) => nameof(Queryable.ThenByDescending),
            (false, _) => nameof(Queryable.ThenBy),
        };

        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Issue), property.Type);

        return (IOrderedQueryable<Issue>)method.Invoke(null, [query, keySelector])!;
    }

    private static Expression BuildNode(FilterNode node, ParameterExpression parameter) =>
        node switch
        {
            FilterGroup group => BuildGroup(group, parameter),
            FilterExpression expression => BuildExpression(expression, parameter),
            _ => throw new NotSupportedException($"Unsupported filter node '{node.GetType().Name}'."),
        };

    private static Expression BuildGroup(FilterGroup group, ParameterExpression parameter)
    {
        Expression? combined = null;
        foreach (var child in group.Nodes)
        {
            var childExpression = BuildNode(child, parameter);
            combined = combined is null
                ? childExpression
                : group.Logic == FilterLogic.And
                    ? Expression.AndAlso(combined, childExpression)
                    : Expression.OrElse(combined, childExpression);
        }

        return combined ?? Expression.Constant(true);
    }

    private static Expression BuildExpression(FilterExpression expression, ParameterExpression parameter)
    {
        var member = FieldMap[expression.Field];
        var property = Expression.Property(parameter, member);
        var value = CoerceValue(expression.Value, property.Type);
        var constant = Expression.Constant(value, property.Type);

        return expression.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(property, constant),
            FilterOperator.NotEquals => Expression.NotEqual(property, constant),
            FilterOperator.GreaterThan => Expression.GreaterThan(property, constant),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
            FilterOperator.LessThan => Expression.LessThan(property, constant),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
            FilterOperator.Contains => BuildStringCall(property, constant, nameof(string.Contains)),
            FilterOperator.StartsWith => BuildStringCall(property, constant, nameof(string.StartsWith)),
            FilterOperator.EndsWith => BuildStringCall(property, constant, nameof(string.EndsWith)),
            _ => throw new NotSupportedException($"Unsupported filter operator '{expression.Operator}'."),
        };
    }

    private static Expression BuildStringCall(Expression property, Expression constant, string methodName)
    {
        var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
        return Expression.Call(property, method, constant);
    }

    private static object? CoerceValue(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null) return null;
        if (underlying.IsInstanceOfType(value)) return value;

        var text = value.ToString() ?? string.Empty;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return underlying switch
        {
            { IsEnum: true } => Enum.Parse(underlying, text, ignoreCase: true),
            _ when underlying == typeof(Guid) => Guid.Parse(text),
            _ when underlying == typeof(DateTimeOffset) => DateTimeOffset.Parse(text, culture),
            _ => Convert.ChangeType(value, underlying, culture),
        };
    }
}
