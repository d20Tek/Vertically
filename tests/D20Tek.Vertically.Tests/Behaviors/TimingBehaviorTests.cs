namespace D20Tek.Vertically.Tests.Behaviors;

[TestClass]
public sealed class TimingBehaviorTests
{
    [TestMethod]
    public async Task HandleAsync_Success_LogsElapsedInformation()
    {
        // Arrange
        var logger = new FakeLogger<TimingBehavior<SampleCommand, string>>();
        var behavior = new TimingBehavior<SampleCommand, string>(logger);

        // Act
        await behavior.HandleAsync(
            new SampleCommand("x"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.HasCount(1, logger.Entries);
        Assert.AreEqual(LogLevel.Information, logger.Entries[0].Level);
        Assert.Contains("SampleCommand", logger.Entries[0].Message);
    }

    [TestMethod]
    public async Task HandleAsync_HandlerThrows_LogsElapsedInFinallyAndRethrows()
    {
        // Arrange
        var logger = new FakeLogger<TimingBehavior<SampleCommand, string>>();
        var behavior = new TimingBehavior<SampleCommand, string>(logger);
        RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("boom");

        // Act - Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            [ExcludeFromCodeCoverage]() => behavior.HandleAsync(new SampleCommand("x"), next, CancellationToken.None));
        Assert.HasCount(1, logger.Entries);
        Assert.Contains("SampleCommand", logger.Entries[0].Message);
    }

    [TestMethod]
    public async Task HandleAsync_InformationDisabled_DoesNotLog()
    {
        // Arrange
        var logger = new FakeLogger<TimingBehavior<SampleCommand, string>> { MinLevel = LogLevel.Warning };
        var behavior = new TimingBehavior<SampleCommand, string>(logger);

        // Act
        await behavior.HandleAsync(
            new SampleCommand("x"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsEmpty(logger.Entries);
    }

    [TestMethod]
    public async Task HandleAsync_NullLogger_DoesNotThrow()
    {
        // Arrange
        var behavior = new TimingBehavior<SampleCommand, string>();

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }
}
