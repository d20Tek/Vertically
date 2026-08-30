namespace D20Tek.Vertically.Tests.Registration;

/// <summary>
/// Covers the per-handler <see cref="IHandlerBehaviorScope"/> convenience methods, ensuring each
/// registers its corresponding built-in behavior into the handler's pipeline.
/// </summary>
[TestClass]
public sealed class HandlerBehaviorScopeConvenienceTests
{
    [TestMethod]
    public void ForCommand_AddLogging_RegistersLoggingBehaviorForHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.ForCommand<SampleCommand>().AddLogging();
        });

        // Assert
        var closed = typeof(LoggingBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        Assert.ContainsSingle(services.Where(d => d.ServiceType == closed));
    }

    [TestMethod]
    public void ForCommand_AddExceptionToResult_RegistersExceptionToResultBehaviorForHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.ForCommand<SampleCommand>().AddExceptionToResult();
        });

        // Assert
        var closed = typeof(ExceptionToResultBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        Assert.ContainsSingle(services.Where(d => d.ServiceType == closed));
    }

    [TestMethod]
    public void ForCommand_AddValidation_RegistersValidationBehaviorForHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.ForCommand<SampleCommand>().AddValidation();
        });

        // Assert
        var closed = typeof(ValidationBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        var descriptor = Assert.ContainsSingle(services.Where(d => d.ServiceType == closed));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
