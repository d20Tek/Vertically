namespace D20Tek.Vertically.Tests.Behaviors;

public sealed partial class ValidationBehaviorTests
{
    [TestMethod]
    public async Task HandleAsync_AsyncValidatorPasses_InvokesNext()
    {
        // Arrange
        var validator = new SampleCommandAsyncValidator { ShouldFail = false };
        var behavior = new ValidationBehavior<SampleCommand, string>(ProviderWithAsync(validator));
        var nextCalled = false;

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok"), () => nextCalled = true),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, validator.CallCount);
    }

    [TestMethod]
    public async Task HandleAsync_AsyncValidatorFails_ShortCircuitsWithoutInvokingNext()
    {
        // Arrange
        var validator = new SampleCommandAsyncValidator { ShouldFail = true };
        var behavior = new ValidationBehavior<SampleCommand, string>(ProviderWithAsync(validator));
        var nextCalled = false;

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok"), [ExcludeFromCodeCoverage]() => nextCalled = true),
            CancellationToken.None);

        // Assert
        Assert.IsFalse(nextCalled);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(1, validator.CallCount);
    }

    [TestMethod]
    public async Task HandleAsync_FirstAsyncValidatorFails_DoesNotRunSecondAsyncValidator()
    {
        // Arrange
        var first = new SampleCommandAsyncValidator { ShouldFail = true };
        var second = new SecondSampleCommandAsyncValidator { ShouldFail = false };
        var behavior = new ValidationBehavior<SampleCommand, string>(ProviderWithAsync(first, second));

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(1, first.CallCount);
        Assert.AreEqual(0, second.CallCount);
    }

    [TestMethod]
    public async Task HandleAsync_SyncValidatorFails_DoesNotRunAsyncValidator()
    {
        // Arrange
        var sync = new SampleCommandValidator { ShouldFail = true };
        var async = new SampleCommandAsyncValidator { ShouldFail = false };
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleCommand>>(sync);
        services.AddSingleton<IAsyncValidator<SampleCommand>>(async);
        var behavior = new ValidationBehavior<SampleCommand, string>(services.BuildServiceProvider());

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok")),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(1, sync.CallCount);
        Assert.AreEqual(0, async.CallCount);
    }

    [TestMethod]
    public async Task HandleAsync_SyncPassesAndAsyncFails_ShortCircuitsAfterAsync()
    {
        // Arrange
        var sync = new SampleCommandValidator { ShouldFail = false };
        var async = new SampleCommandAsyncValidator { ShouldFail = true };
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleCommand>>(sync);
        services.AddSingleton<IAsyncValidator<SampleCommand>>(async);
        var behavior = new ValidationBehavior<SampleCommand, string>(services.BuildServiceProvider());
        var nextCalled = false;

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok"), [ExcludeFromCodeCoverage]() => nextCalled = true),
            CancellationToken.None);

        // Assert
        Assert.IsFalse(nextCalled);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(1, sync.CallCount);
        Assert.AreEqual(1, async.CallCount);
    }

    [TestMethod]
    public async Task HandleAsync_SyncAndAsyncValidatorsPass_RunsBothAndInvokesNext()
    {
        // Arrange
        var sync = new SampleCommandValidator { ShouldFail = false };
        var async = new SampleCommandAsyncValidator { ShouldFail = false };
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleCommand>>(sync);
        services.AddSingleton<IAsyncValidator<SampleCommand>>(async);
        var behavior = new ValidationBehavior<SampleCommand, string>(services.BuildServiceProvider());
        var nextCalled = false;

        // Act
        var result = await behavior.HandleAsync(
            new SampleCommand("x"),
            Next(Result<string>.Success("ok"), () => nextCalled = true),
            CancellationToken.None);

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, sync.CallCount);
        Assert.AreEqual(1, async.CallCount);
    }
}
