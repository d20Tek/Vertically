namespace D20Tek.Vertically.Registration;

using D20Tek.Vertically.Behaviors;

internal sealed class BehaviorRegistrationBuilder(VerticallyBuilder builder) : IBehaviorRegistrationBuilder
{
    private readonly VerticallyBuilder _builder = builder;

    public IBehaviorRegistrationBuilder Add(Type openGenericBehaviorType)
    {
        BehaviorTypeValidator.EnsureOpenGenericPipelineBehavior(openGenericBehaviorType, nameof(openGenericBehaviorType));

        _builder.AddGlobalBehavior(openGenericBehaviorType);
        return this;
    }

    public IBehaviorRegistrationBuilder AddLogging() => Add(typeof(LoggingBehavior<,>));

    public IBehaviorRegistrationBuilder AddTiming() => Add(typeof(TimingBehavior<,>));

    public IBehaviorRegistrationBuilder AddExceptionToResult() => Add(typeof(ExceptionToResultBehavior<,>));

    public IBehaviorRegistrationBuilder AddValidation() => Add(typeof(ValidationBehavior<,>));
}
