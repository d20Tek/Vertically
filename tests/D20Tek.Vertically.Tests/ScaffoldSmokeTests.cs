namespace D20Tek.Vertically.Tests;

[TestClass]
public sealed class ScaffoldSmokeTests
{
    [TestMethod]
    public void TestProject_IsWiredUp_CanReferenceLibraryTypes()
    {
        // Arrange
        var command = new Fakes.SampleCommand("hello");

        // Act
        var isCommand = command is ICommand<string>;

        // Assert
        Assert.IsTrue(isCommand);
    }
}

