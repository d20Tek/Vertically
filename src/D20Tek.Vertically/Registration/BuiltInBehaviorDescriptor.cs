namespace D20Tek.Vertically.Registration;

using D20Tek.Vertically.Behaviors;

/// <summary>
/// Descriptor for the built-in behaviors. Closes each behavior through generic code
/// (for example <c>LoggingBehavior&lt;TRequest, TResult&gt;</c>), so built-in behaviors are
/// trim- and AOT-safe regardless of how they were added.
/// </summary>
internal sealed class BuiltInBehaviorDescriptor : BehaviorDescriptor
{
    public static readonly BuiltInBehaviorDescriptor Logging = new(BuiltInKind.Logging, typeof(LoggingBehavior<,>));

    public static readonly BuiltInBehaviorDescriptor Timing = new(BuiltInKind.Timing, typeof(TimingBehavior<,>));

    public static readonly BuiltInBehaviorDescriptor ExceptionToResult =
        new(BuiltInKind.ExceptionToResult, typeof(ExceptionToResultBehavior<,>));

    public static readonly BuiltInBehaviorDescriptor Validation =
        new(BuiltInKind.Validation, typeof(ValidationBehavior<,>));

    private static readonly BuiltInBehaviorDescriptor[] All = [Logging, Timing, ExceptionToResult, Validation];

    private readonly BuiltInKind _kind;

    private BuiltInBehaviorDescriptor(BuiltInKind kind, Type identity)
    {
        _kind = kind;
        Identity = identity;
    }

    private enum BuiltInKind
    {
        Logging,
        Timing,
        ExceptionToResult,
        Validation,
    }

    public override Type Identity { get; }

    /// <summary>Returns the built-in descriptor for an open generic definition, if it is one.</summary>
    public static BuiltInBehaviorDescriptor? Find(Type openGenericBehaviorType) =>
        Array.Find(All, d => d.Identity == openGenericBehaviorType);

    public override Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>> Bind<TRequest, TResult>(
        IServiceCollection services) =>
        _kind switch
        {
            BuiltInKind.Logging => RegisterBehavior<LoggingBehavior<TRequest, TResult>, TRequest, TResult>(services),
            BuiltInKind.Timing => RegisterBehavior<TimingBehavior<TRequest, TResult>, TRequest, TResult>(services),
            BuiltInKind.ExceptionToResult =>
                RegisterBehavior<ExceptionToResultBehavior<TRequest, TResult>, TRequest, TResult>(services),
            _ => RegisterBehavior<ValidationBehavior<TRequest, TResult>, TRequest, TResult>(services),
        };
}
