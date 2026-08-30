namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class BehaviorRegistrationValidationTests
{
    [TestMethod]
    public void Add_ClosedGenericBehaviorType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.Behaviors.Add(typeof(LoggingBehavior<SampleCommand, string>))));
        Assert.Contains("open generic type definition", ex.Message);
    }

    [TestMethod]
    public void Add_OpenGenericTypeNotImplementingPipelineBehavior_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.Behaviors.Add(typeof(NotABehavior<,>))));
        Assert.Contains("IPipelineBehavior", ex.Message);
    }

    [TestMethod]
    public void Add_OpenGenericTypeImplementingWrongGenericInterface_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.Behaviors.Add(typeof(WrongInterfaceBehavior<,>))));
        Assert.Contains("IPipelineBehavior", ex.Message);
    }

    [TestMethod]
    public void Add_OpenGenericTypeImplementingOnlyNonGenericInterface_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.Behaviors.Add(typeof(NonGenericInterfaceBehavior<,>))));
        Assert.Contains("IPipelineBehavior", ex.Message);
    }

    [TestMethod]
    public void ForCommandAdd_OpenGenericTypeNotImplementingPipelineBehavior_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.ForCommand<SampleCommand>().Add(typeof(NotABehavior<,>))));
        Assert.Contains("IPipelineBehavior", ex.Message);
    }

    [TestMethod]
    public void ForCommandInsertBefore_AnchorNotImplementingPipelineBehavior_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        var ex = Assert.ThrowsExactly<ArgumentException>([ExcludeFromCodeCoverage]() =>
            services.AddVertically([ExcludeFromCodeCoverage](b) =>
                b.ForCommand<SampleCommand>()
                    .InsertBefore(typeof(NotABehavior<,>))
                    .Add(typeof(LoggingBehavior<,>))));
        Assert.Contains("IPipelineBehavior", ex.Message);
    }
}
