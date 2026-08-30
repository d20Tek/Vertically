namespace D20Tek.Vertically;

/// <summary>
/// Represents a validator that asynchronously validates an input of type T and returns validation
/// errors if any. Use this contract when validation requires asynchronous work (for example I/O or
/// remote calls); use <see cref="IValidator{T}"/> for synchronous validation.
/// </summary>
/// <typeparam name="T">The type of the input to validate.</typeparam>
public interface IAsyncValidator<in T> where T : notnull
{
    /// <summary>
    /// Validates the specified input asynchronously and returns any validation errors.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that resolves to a ValidationErrors object containing any validation errors.
    /// </returns>
    Task<ValidationErrors> ValidateAsync(T input, CancellationToken cancellationToken = default);
}
