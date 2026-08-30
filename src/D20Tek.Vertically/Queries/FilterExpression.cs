namespace D20Tek.Vertically.Queries;

/// <summary>
/// A provider-agnostic filter instruction. Adapters resolve <paramref name="Field"/> against their
/// own model, coerce <paramref name="Value"/>, and apply the given <paramref name="Operator"/>.
/// </summary>
/// <param name="Field">The name of the field to filter on.</param>
/// <param name="Operator">The comparison to apply.</param>
/// <param name="Value">The value to compare against.</param>
public sealed record FilterExpression(string Field, FilterOperator Operator, object? Value);
