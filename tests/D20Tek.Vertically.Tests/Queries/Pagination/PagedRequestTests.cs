namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class PagedRequestTests
{
    [TestMethod]
    public void Defaults_MatchConstants()
    {
        // Arrange & Act
        var request = new PagedRequest();

        // Assert
        Assert.AreEqual(1, request.PageNumber);
        Assert.AreEqual(20, request.PageSize);
    }

    [TestMethod]
    [DataRow(1, 20, 0, 20)]
    [DataRow(2, 20, 20, 20)]
    [DataRow(3, 50, 100, 50)]
    [DataRow(5, 10, 40, 10)]
    public void SkipAndTake_ComputeFromPageNumberAndSize(
        int pageNumber, int pageSize, int expectedSkip, int expectedTake)
    {
        // Arrange
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize };

        // Act & Assert
        Assert.AreEqual(expectedSkip, request.Skip);
        Assert.AreEqual(expectedTake, request.Take);
    }

    [TestMethod]
    public void Request_ImplementsPagedRequestMarker()
    {
        // Arrange & Act
        var request = new PagedRequest();

        // Assert
        Assert.IsInstanceOfType<IPagedRequest>(request);
    }
}
