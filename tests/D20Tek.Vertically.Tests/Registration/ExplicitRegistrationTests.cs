using System.Diagnostics.CodeAnalysis;

namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class ExplicitRegistrationTests
{
    [TestMethod]
    public void AddCommandHandler_RegistersHandlerServiceAndImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddCommandHandler<SampleCommandHandler>());
        using var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<ICommandHandler<SampleCommand, string>>();
        Assert.IsNotNull(handler);
        Assert.IsInstanceOfType<SampleCommandHandler>(handler);
    }

    [TestMethod]
    public async Task AddCommandHandler_ResolvedHandler_ExecutesAndReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b => b.Handlers.AddCommandHandler<SampleCommandHandler>());
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<SampleCommand, string>>();

        // Act
        var result = await handler.HandleAsync(new SampleCommand("abc"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("abc", result.GetValue());
    }

    [TestMethod]
    public void AddQueryHandler_RegistersHandlerServiceAndImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddQueryHandler<SampleQueryHandler>());
        using var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IQueryHandler<SampleQuery, string>>();
        Assert.IsNotNull(handler);
        Assert.IsInstanceOfType<SampleQueryHandler>(handler);
    }

    [TestMethod]
    public void AddCommandHandler_TypeIsNotCommandHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) => b.Handlers.AddCommandHandler<SampleQueryHandler>()));
        Assert.Contains("ICommandHandler", ex.Message);
    }

    [TestMethod]
    public void AddQueryHandler_TypeIsNotQueryHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) => b.Handlers.AddQueryHandler<SampleCommandHandler>()));
        Assert.Contains("IQueryHandler", ex.Message);
    }

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

    [TestMethod]
    public void AddCommandHandler_SameHandlerRegisteredTwice_DedupesAndDoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<ICommandHandler<SampleCommand, string>>().ToArray();
        Assert.HasCount(1, handlers);
        Assert.IsInstanceOfType<SampleCommandHandler>(handlers[0]);
    }

    [TestMethod]
    public void AddCommandHandler_TwoDifferentHandlersForSameRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
            {
                b.Handlers.AddCommandHandler<ConflictHandlerA<ScanExcluded>>();
                b.Handlers.AddCommandHandler<ConflictHandlerB<ScanExcluded>>();
            }));
        Assert.Contains("Only one handler may be registered per request type.", ex.Message);
    }
}
