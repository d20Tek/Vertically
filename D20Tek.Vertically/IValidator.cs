using D20Tek.Functional;

namespace D20Tek.Vertically;

/// <summary>
/// Represents a validator that can validate an input of type T and return validation errors if any.
/// </summary>
/// <typeparam name="T">The type of the input to validate.</typeparam>
public interface IValidator<in T> where T : notnull
{
    /// <summary>
    /// Validates the specified input and returns any validation errors.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A ValidationErrors object containing any validation errors.</returns>
    ValidationErrors Validate(T input);
}
