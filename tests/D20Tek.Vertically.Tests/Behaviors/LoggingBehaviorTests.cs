namespace D20Tek.Vertically.Tests.Behaviors;

[TestClass]
public sealed class LoggingBehaviorTests
{
    private static RequestHandlerDelegate<string> Next(Result<string> result) => () => Task.FromResult(result);

    [TestMethod]
    public async Task HandleAsync_Success_LogsStartAndSuccessInformation()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<SampleCommand, string>>();
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);

        // Act
        await behavior.HandleAsync(new SampleCommand("x"), Next(Result<string>.Success("ok")), CancellationToken.None);

        // Assert
        Assert.HasCount(2, logger.Entries);
        Assert.AreEqual(LogLevel.Information, logger.Entries[0].Level);
        Assert.Contains("SampleCommand", logger.Entries[0].Message);
        Assert.Contains("successfully", logger.Entries[1].Message);
    }

    [TestMethod]
    public async Task HandleAsync_Failure_LogsStartInformationAndWarningWithErrors()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<SampleCommand, string>>();
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);
        var failure = Result<string>.Failure(Error.Validation("code.x", "boom"));

        // Act
        await behavior.HandleAsync(new SampleCommand("x"), Next(failure), CancellationToken.None);

        // Assert
        Assert.HasCount(2, logger.Entries);
        Assert.AreEqual(LogLevel.Information, logger.Entries[0].Level);
        Assert.AreEqual(LogLevel.Warning, logger.Entries[1].Level);
        Assert.Contains("code.x", logger.Entries[1].Message);
        Assert.Contains("boom", logger.Entries[1].Message);
    }

    [TestMethod]
    public async Task HandleAsync_InformationDisabled_DoesNotLogStartOrSuccess()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<SampleCommand, string>> { MinLevel = LogLevel.Warning };
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);

        // Act
        await behavior.HandleAsync(new SampleCommand("x"), Next(Result<string>.Success("ok")), CancellationToken.None);

        // Assert
        Assert.IsEmpty(logger.Entries);
    }

    [TestMethod]
    public async Task HandleAsync_WarningDisabled_DoesNotLogFailure()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<SampleCommand, string>> { MinLevel = LogLevel.None };
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);
        var failure = Result<string>.Failure(Error.Validation("code.x", "boom"));

        // Act
        await behavior.HandleAsync(new SampleCommand("x"), Next(failure), CancellationToken.None);

        // Assert
        Assert.IsEmpty(logger.Entries);
    }

    [TestMethod]
    public async Task HandleAsync_NullLogger_DoesNotThrow()
    {
        // Arrange
        var behavior = new LoggingBehavior<SampleCommand, string>();

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"), 
            Next(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }
}
