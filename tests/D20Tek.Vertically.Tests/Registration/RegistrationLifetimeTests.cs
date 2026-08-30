namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class RegistrationLifetimeTests
{
    [TestMethod]
    public void AddCommandHandler_HandlerImplementation_RegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddCommandHandler<SampleCommandHandler>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(SampleCommandHandler));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddCommandHandler_NoBehaviors_ServiceInterfaceRegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddCommandHandler<SampleCommandHandler>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(ICommandHandler<SampleCommand, string>));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddValidator_Validator_RegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddValidator<SampleCommandValidator>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(IValidator<SampleCommand>));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddBehavior_NonScopedBehavior_RegisteredSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Behaviors.AddLogging();
        });

        // Assert
        var closed = typeof(LoggingBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        var descriptor = services.Single(d => d.ServiceType == closed);
        Assert.AreEqual(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddBehavior_ScopedBehavior_RegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Behaviors.AddValidation();
        });

        // Assert
        var closed = typeof(ValidationBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        var descriptor = services.Single(d => d.ServiceType == closed);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddBehavior_WithBehaviors_ServiceInterfaceRegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Behaviors.AddLogging();
        });

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(ICommandHandler<SampleCommand, string>));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
