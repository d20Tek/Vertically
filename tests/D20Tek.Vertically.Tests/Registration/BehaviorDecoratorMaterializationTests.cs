namespace D20Tek.Vertically.Tests.Registration;

/// <summary>
/// Directly exercises the behavior-decorator constructors to cover the
/// <c>behaviors as IReadOnlyList&lt;...&gt; ?? [.. behaviors]</c> fallback, which the DI composer
/// never triggers because it always supplies an array. A lazy iterator (non-IReadOnlyList)
/// forces materialization via the collection expression.
/// </summary>
[TestClass]
public sealed class BehaviorDecoratorMaterializationTests
{
    [TestInitialize]
    public void Initialize() => ExecutionLog.Clear();

    private static IEnumerable<IPipelineBehavior<TRequest, TResult>> Lazy<TRequest, TResult>(
        params IPipelineBehavior<TRequest, TResult>[] behaviors)
        where TRequest : notnull
        where TResult : notnull
    {
        foreach (var behavior in behaviors)
        {
            yield return behavior;
        }
    }

    [TestMethod]
    public async Task CommandDecorator_NonListBehaviors_MaterializesAndInvokesInOrder()
    {
        // Arrange
        var behaviors = Lazy<OrderingCommand, string>(
            new FirstBehavior<OrderingCommand, string>(),
            new SecondBehavior<OrderingCommand, string>());
        var decorator = new CommandHandlerBehaviorDecorator<OrderingCommand, string>(
            new OrderingCommandHandler(), behaviors);

        // Act
        var result = await decorator.HandleAsync(new OrderingCommand("x"));

        // Assert
        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(
            new[] { "First:before", "Second:before", "Handler", "Second:after", "First:after" },
            ExecutionLog.Entries.ToArray());
    }

    [TestMethod]
    public async Task QueryDecorator_NonListBehaviors_MaterializesAndInvokesInOrder()
    {
        // Arrange
        var behaviors = Lazy<OrderingQuery, string>(
            new FirstBehavior<OrderingQuery, string>(),
            new SecondBehavior<OrderingQuery, string>());
        var decorator = new QueryHandlerBehaviorDecorator<OrderingQuery, string>(
            new OrderingQueryHandler(), behaviors);

        // Act
        var result = await decorator.HandleAsync(new OrderingQuery("x"));

        // Assert
        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(
            new[] { "First:before", "Second:before", "Handler", "Second:after", "First:after" },
            ExecutionLog.Entries.ToArray());
    }
}
