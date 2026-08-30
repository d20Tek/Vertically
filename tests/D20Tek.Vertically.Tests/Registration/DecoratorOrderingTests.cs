namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class DecoratorOrderingTests
{
    [TestInitialize]
    public void Initialize() => ExecutionLog.Clear();

    [TestMethod]
    public async Task GlobalBehaviors_WrapHandler_InRegistrationOrderOutermostFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.Behaviors.Add(typeof(SecondBehavior<,>));
            b.Behaviors.Add(typeof(ThirdBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.AreSequenceEqual(
            [
                "First:before", "Second:before", "Third:before",
                "Handler",
                "Third:after", "Second:after", "First:after",
            ], 
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task PerHandlerBehavior_IsInnermostByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.ForCommand<OrderingCommand>().Add(typeof(SecondBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.AreSequenceEqual(
            ["First:before", "Second:before", "Handler", "Second:after", "First:after"],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task PerHandlerBehavior_AtOutermost_WrapsGlobalBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.ForCommand<OrderingCommand>().AtOutermost().Add(typeof(SecondBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.AreSequenceEqual(
            ["Second:before", "First:before", "Handler", "First:after", "Second:after"],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task PerHandlerBehavior_InsertBefore_PlacesBehaviorBeforeAnchor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.Behaviors.Add(typeof(SecondBehavior<,>));
            b.ForCommand<OrderingCommand>()
                .InsertBefore(typeof(SecondBehavior<,>))
                .Add(typeof(ThirdBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.AreSequenceEqual(
            [
                "First:before", "Third:before", "Second:before",
                "Handler",
                "Second:after", "Third:after", "First:after"
            ],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public void PerHandlerBehavior_InsertBeforeMissingAnchor_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
            {
                b.Handlers.AddCommandHandler<OrderingCommandHandler>();
                b.Behaviors.Add(typeof(FirstBehavior<,>));
                b.ForCommand<OrderingCommand>()
                    .InsertBefore(typeof(SecondBehavior<,>))
                    .Add(typeof(ThirdBehavior<,>));
            }));
        Assert.Contains("anchor behavior is not part of this handler's pipeline", ex.Message);
    }

    [TestMethod]
    public void NoBehaviors_ServiceInterface_MapsDirectlyToImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b => b.Handlers.AddCommandHandler<OrderingCommandHandler>());

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(ICommandHandler<OrderingCommand, string>));
        Assert.AreEqual(typeof(OrderingCommandHandler), descriptor.ImplementationType);
        Assert.IsNull(descriptor.ImplementationFactory);
    }

    [TestMethod]
    public async Task ShortCircuitBehavior_ReturnsFailure_WithoutInvokingHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler>();
            b.Behaviors.Add(typeof(ShortCircuitBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OrderingCommand, string>>();

        // Act
        var result = await handler.HandleAsync(new OrderingCommand("x"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreSequenceEqual(["ShortCircuit:short-circuited"], [.. ExecutionLog.Entries]);
    }
}
