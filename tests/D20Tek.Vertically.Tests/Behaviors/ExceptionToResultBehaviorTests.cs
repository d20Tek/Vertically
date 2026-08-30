namespace D20Tek.Vertically.Tests.Behaviors;

[TestClass]
public sealed class ExceptionToResultBehaviorTests
{
    [TestMethod]
    public async Task HandleAsync_HandlerThrows_MapsExceptionToFailureResult()
    {
        // Arrange
        var behavior = new ExceptionToResultBehavior<SampleCommand, string>();
        RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("boom");

        // Act
        var result = await behavior.HandleAsync(new SampleCommand("x"), next, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public async Task HandleAsync_HandlerSucceeds_PassesResultThrough()
    {
        // Arrange
        var behavior = new ExceptionToResultBehavior<SampleCommand, string>();

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("ok", result.GetValue());
    }

    [TestMethod]
    public async Task HandleAsync_OperationCanceled_RethrowsInsteadOfMapping()
    {
        // Arrange
        var behavior = new ExceptionToResultBehavior<SampleCommand, string>();
        RequestHandlerDelegate<string> next = () => throw new OperationCanceledException();

        // Act - Assert
        await Assert.ThrowsExactlyAsync<OperationCanceledException>([ExcludeFromCodeCoverage]() => 
            behavior.HandleAsync(new SampleCommand("x"), next, CancellationToken.None));
    }
}
