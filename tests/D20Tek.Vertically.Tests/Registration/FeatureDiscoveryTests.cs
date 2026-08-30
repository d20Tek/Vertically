namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class FeatureDiscoveryTests
{
    private static Assembly TestAssembly => typeof(SampleFeature).Assembly;

    [TestMethod]
    public void RegisterFromAssembly_DiscoversFeatureRegisteredHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        // The feature configures a per-handler behavior, so the resolved handler is a decorator.
        var handler = provider.GetService<ICommandHandler<SampleFeature.Command, string>>();
        Assert.IsNotNull(handler);
        Assert.IsNotNull(provider.GetService<SampleFeature.Handler>());
    }

    [TestMethod]
    public void RegisterFromAssembly_DiscoversFeatureRegisteredValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IValidator<SampleFeature.Command>>();
        Assert.IsNotNull(validator);
        Assert.IsInstanceOfType<SampleFeature.Validator>(validator);
    }

    [TestMethod]
    public void RegisterFromAssembly_FeatureOwnedTypes_RegisteredExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var validators = provider.GetServices<IValidator<SampleFeature.Command>>().ToArray();
        Assert.HasCount(1, validators, "Nested feature-owned validator should not be re-registered by the loose scan.");
    }

    [TestMethod]
    public void RegisterFromAssembly_ExplicitFeatureRegistration_MatchesScanDiscovery()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => new SampleFeature().Register(b));
        using var provider = services.BuildServiceProvider();

        // Assert
        // The feature configures a per-handler behavior, so the resolved handler is a decorator.
        var handler = provider.GetService<ICommandHandler<SampleFeature.Command, string>>();
        Assert.IsNotNull(handler);
        Assert.IsNotNull(provider.GetService<SampleFeature.Handler>());
    }
}
