namespace D20Tek.Vertically.Tests.Registration;

/// <summary>
/// Covers the global <see cref="IBehaviorRegistrationBuilder"/> convenience methods, ensuring each
/// registers its corresponding built-in behavior into the handler's pipeline.
/// </summary>
[TestClass]
public sealed class BehaviorRegistrationBuilderConvenienceTests
{
    [TestMethod]
    public void AddTiming_RegistersTimingBehaviorForHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Behaviors.AddTiming();
        });

        // Assert
        var closed = typeof(TimingBehavior<,>).MakeGenericType(typeof(SampleCommand), typeof(string));
        Assert.ContainsSingle(services.Where(d => d.ServiceType == closed));
    }
}
