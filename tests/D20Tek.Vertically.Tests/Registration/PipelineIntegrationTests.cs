namespace D20Tek.Vertically.Tests.Registration;

/// <summary>
/// End-to-end tests that resolve handlers from a built provider and exercise the full pipeline,
/// closing coverage on the query decorator path and the ExceptionToResult behavior wired through
/// real registration.
/// </summary>
[TestClass]
public sealed class PipelineIntegrationTests
{
    [TestInitialize]
    public void Initialize() => ExecutionLog.Clear();

    [TestMethod]
    public async Task QueryPipeline_GlobalBehaviors_WrapHandlerOutermostFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddQueryHandler<OrderingQueryHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.Behaviors.Add(typeof(SecondBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<OrderingQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new OrderingQuery("q"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreSequenceEqual(
            ["First:before", "Second:before", "Handler", "Second:after", "First:after"],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task QueryPipeline_PerQueryBehavior_IsInnermostByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddQueryHandler<OrderingQueryHandler>();
            b.Behaviors.Add(typeof(FirstBehavior<,>));
            b.ForQuery<OrderingQuery>().Add(typeof(SecondBehavior<,>));
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<OrderingQuery, string>>();

        // Act
        await handler.HandleAsync(new OrderingQuery("q"), CancellationToken.None);

        // Assert
        Assert.AreSequenceEqual(
            ["First:before", "Second:before", "Handler", "Second:after", "First:after"],
            [.. ExecutionLog.Entries]);
    }

    [TestMethod]
    public async Task CommandPipeline_ThrowingHandler_MappedToFailureByExceptionToResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<ThrowingCommandHandler>();
            b.Behaviors.AddExceptionToResult();
        });
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<ThrowingCommand, string>>();

        // Act
        var result = await handler.HandleAsync(new ThrowingCommand("x"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public async Task CommandPipeline_ValidationFails_ShortCircuitsThroughResolvedHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Handlers.AddValidator<FailingSampleCommandValidator>();
            b.Behaviors.AddValidation();
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SampleCommand, string>>();

        // Act
        var result = await handler.HandleAsync(new SampleCommand("x"), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public async Task CommandPipeline_SecondCommandHandler_ResolvesAndExecutesIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVertically(b => b.Handlers.AddCommandHandler<OtherCommandHandler>());
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<OtherCommand, int>>();

        // Act
        var result = await handler.HandleAsync(new OtherCommand(42), CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(42, result.GetValue());
    }
}
