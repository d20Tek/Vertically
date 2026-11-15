namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class TypedHandlerRegistrationTests
{
    [TestInitialize]
    public void Initialize() => ExecutionLog.Clear();

    [TestMethod]
    public void AddCommandHandlerTyped_NoBehaviors_MapsServiceDirectlyToImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(ICommandHandler<OrderingCommand, string>));
        Assert.AreEqual(typeof(OrderingCommandHandler), descriptor.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddQueryHandlerTyped_NoBehaviors_MapsServiceDirectlyToImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddQueryHandler<OrderingQueryHandler, OrderingQuery, string>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(IQueryHandler<OrderingQuery, string>));
        Assert.AreEqual(typeof(OrderingQueryHandler), descriptor.ImplementationType);
    }

    [TestMethod]
    public async Task AddCommandHandlerTyped_WithBehaviors_ComposesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
            b.Behaviors.Add<FirstBehavior<OrderingCommand, string>>();
            b.Behaviors.AddTiming();
            b.ForCommand<OrderingCommand>().Add<SecondBehavior<OrderingCommand, string>>();
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        var result = await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreSequenceEqual(
            ["First:before", "Second:before", "Handler", "Second:after", "First:after"],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task AddQueryHandlerTyped_WithBehaviors_ComposesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddQueryHandler<OrderingQueryHandler, OrderingQuery, string>();
            b.Behaviors.AddLogging().AddExceptionToResult().AddValidation();
            b.ForQuery<OrderingQuery>().AtOutermost().Add<FirstBehavior<OrderingQuery, string>>();
        });
        services.AddSingleton(typeof(ILogger<>), typeof(FakeLogger<>));
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<OrderingQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new OrderingQuery("q"), CancellationToken.None);

        // Assert
        Assert.AreEqual("q", result.GetValue());
        Assert.AreSequenceEqual(["First:before", "Handler", "First:after"], [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public void AddFeature_RegistersFeatureHandlers_WithFeatureSource()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddFeature<TypedFeature>();
            b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
            captured = b;
        });

        // Assert
        var registrations = captured!.HandlerRegistrations;
        Assert.HasCount(2, registrations);
        Assert.AreEqual(RegistrationSource.Feature, registrations[0].Source);
        Assert.AreEqual(typeof(TypedFeature.Handler), registrations[0].ImplementationType);
        Assert.AreEqual(RegistrationSource.Manual, registrations[1].Source);
        Assert.Contains(d => d.ServiceType == typeof(ICommandHandler<TypedFeature.Command, string>), services);
    }
}
