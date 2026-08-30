namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class AddVerticallyTests
{
    [TestMethod]
    public void AddVertically_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act - Assert
        Assert.ThrowsExactly<ArgumentNullException>([ExcludeFromCodeCoverage]() => 
            services.AddVertically([ExcludeFromCodeCoverage] (_) => { }));
    }

    [TestMethod]
    public void AddVertically_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Assert
        Assert.ThrowsExactly<ArgumentNullException>([ExcludeFromCodeCoverage]() => services.AddVertically(null!));
    }

    [TestMethod]
    public void AddVertically_ReturnsSameServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddVertically(_ => { });

        // Assert
        Assert.AreSame(services, result);
    }

    [TestMethod]
    public void AddVertically_InvokesConfigureCallbackOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var callCount = 0;

        // Act
        services.AddVertically(_ => callCount++);

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void AddVertically_PassesBuilderExposingServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        IServiceCollection? captured = null;

        // Act
        services.AddVertically(builder => captured = builder.Services);

        // Assert
        Assert.AreSame(services, captured);
    }
}
