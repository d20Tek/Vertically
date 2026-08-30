namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class ExplicitValidatorRegistrationTests
{
    [TestMethod]
    public void AddValidator_RegistersValidatorService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddValidator<SampleCommandValidator>());
        using var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IValidator<SampleCommand>>();
        Assert.IsNotNull(validator);
        Assert.IsInstanceOfType<SampleCommandValidator>(validator);
    }

    [TestMethod]
    public void AddValidator_TypeIsNotValidator_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) => b.Handlers.AddValidator<SampleCommandHandler>()));
        Assert.Contains("IValidator", ex.Message);
    }

    [TestMethod]
    public void AddValidator_DuplicateSameType_RegistersOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddValidator<SampleCommandValidator>();
            b.Handlers.AddValidator<SampleCommandValidator>();
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var validators = provider.GetServices<IValidator<SampleCommand>>().ToArray();
        Assert.HasCount(1, validators);
    }

    [TestMethod]
    public void AddValidator_TwoDifferentValidatorsForSameType_RegistersBoth()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddValidator<SampleCommandValidator>();
            b.Handlers.AddValidator<SecondSampleCommandValidator>();
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var validators = provider.GetServices<IValidator<SampleCommand>>().ToArray();
        Assert.HasCount(2, validators);
    }

    [TestMethod]
    public void AddValidator_AsyncValidator_RegistersAsyncValidatorService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddValidator<SampleCommandAsyncValidator>());
        using var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IAsyncValidator<SampleCommand>>();
        Assert.IsNotNull(validator);
        Assert.IsInstanceOfType<SampleCommandAsyncValidator>(validator);
    }

    [TestMethod]
    public void AddValidator_AsyncValidatorRegisteredScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddValidator<SampleCommandAsyncValidator>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(IAsyncValidator<SampleCommand>));
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
