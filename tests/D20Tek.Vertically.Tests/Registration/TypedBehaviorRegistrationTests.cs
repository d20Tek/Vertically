namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class TypedBehaviorRegistrationTests
{
    [TestInitialize]
    public void Initialize() => ExecutionLog.Clear();

    [TestMethod]
    public async Task GlobalAddTyped_NotApplicableToHandler_IsSkipped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddQueryHandler<OrderingQueryHandler, OrderingQuery, string>();
            b.Behaviors.Add<FirstBehavior<OrderingCommand, string>>();
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<OrderingQuery, string>>();

        // Act
        await handler.HandleAsync(new OrderingQuery("q"), CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<OrderingQueryHandler>(handler);
        Assert.AreSequenceEqual(["Handler"], [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public void GlobalAddTyped_AddedTwice_RegistersOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
            b.Behaviors.Add<FirstBehavior<OrderingCommand, string>>().Add<FirstBehavior<OrderingCommand, string>>();
        });

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(FirstBehavior<OrderingCommand, string>));
        Assert.AreEqual(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [TestMethod]
    public void PerHandlerAddTyped_NotApplicableToHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
            {
                b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
                b.ForCommand<OrderingCommand>().Add<FirstBehavior<OrderingQuery, string>>();
            }));
        Assert.Contains("cannot be applied to request", ex.Message);
    }

    [TestMethod]
    public void GlobalAddTyped_NotABehavior_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) => b.Behaviors.Add<NotAClosedBehavior>()));
    }

    [TestMethod]
    public void PerHandlerAddTyped_NotABehavior_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.ForCommand<OrderingCommand>().Add<NotAClosedBehavior>()));
    }

    [TestMethod]
    public void InsertBefore_ClosedNonBehaviorAnchor_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.ForCommand<OrderingCommand>().InsertBefore(typeof(NotAClosedBehavior))));
    }

    [TestMethod]
    public async Task InsertBefore_TypedAnchor_PlacesBehaviorBeforeAnchor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
            b.Behaviors.Add<FirstBehavior<OrderingCommand, string>>();
            b.Behaviors.Add<SecondBehavior<OrderingCommand, string>>();
            b.ForCommand<OrderingCommand>()
                .InsertBefore(typeof(SecondBehavior<OrderingCommand, string>))
                .Add<ThirdBehavior<OrderingCommand, string>>();
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
                "Second:after", "Third:after", "First:after",
            ],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public void AddOpenGenericBuiltIn_SameAsConvenienceMethod_RegistersOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<OrderingCommandHandler, OrderingCommand, string>();
            b.Behaviors.AddTiming().Add(typeof(D20Tek.Vertically.Behaviors.TimingBehavior<,>));
        });

        // Assert
        Assert.AreEqual(
            1,
            services.Count(d => d.ServiceType == typeof(D20Tek.Vertically.Behaviors.TimingBehavior<OrderingCommand, string>)));
    }
}
