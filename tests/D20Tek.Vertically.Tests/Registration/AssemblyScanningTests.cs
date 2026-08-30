namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class AssemblyScanningTests
{
    private static Assembly TestAssembly => typeof(SampleCommandHandler).Assembly;

    [TestMethod]
    public void RegisterFromAssembly_DiscoversCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<ICommandHandler<SampleCommand, string>>();
        Assert.IsNotNull(handler);
        Assert.IsInstanceOfType<SampleCommandHandler>(handler);
    }

    [TestMethod]
    public void RegisterFromAssembly_DiscoversQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IQueryHandler<SampleQuery, string>>();
        Assert.IsNotNull(handler);
        Assert.IsInstanceOfType<SampleQueryHandler>(handler);
    }

    [TestMethod]
    public void RegisterFromAssembly_DiscoversValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssembly(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var validators = provider.GetServices<IValidator<SampleCommand>>().ToArray();
        Assert.IsTrue(validators.Length >= 2, "Expected both SampleCommand validators to be discovered.");
    }

    [TestMethod]
    public void RegisterFromAssemblies_ScansEachAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssemblies(TestAssembly));
        using var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<ICommandHandler<SampleCommand, string>>();
        Assert.IsNotNull(handler);
    }

    [TestMethod]
    public void RegisterFromAssemblies_NoAssemblies_RegistersNothing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.RegisterFromAssemblies());
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsNull(provider.GetService<ICommandHandler<SampleCommand, string>>());
    }
}
