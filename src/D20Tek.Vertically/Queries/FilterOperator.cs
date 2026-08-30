namespace D20Tek.Vertically.Queries;

/// <summary>
/// The comparison a <see cref="FilterExpression"/> applies to a field.
/// </summary>
public enum FilterOperator
{
    /// <summary>Field equals the value.</summary>
    Equals = 0,

    /// <summary>Field does not equal the value.</summary>
    NotEquals = 1,

    /// <summary>Field is greater than the value.</summary>
    GreaterThan = 2,

    /// <summary>Field is greater than or equal to the value.</summary>
    GreaterThanOrEqual = 3,

    /// <summary>Field is less than the value.</summary>
    LessThan = 4,

    /// <summary>Field is less than or equal to the value.</summary>
    LessThanOrEqual = 5,

    /// <summary>Field contains the value.</summary>
    Contains = 6,

    /// <summary>Field starts with the value.</summary>
    StartsWith = 7,

    /// <summary>Field ends with the value.</summary>
    EndsWith = 8,
}
