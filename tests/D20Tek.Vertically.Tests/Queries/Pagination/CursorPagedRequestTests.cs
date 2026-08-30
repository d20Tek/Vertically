namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class CursorPagedRequestTests
{
    [TestMethod]
    public void Defaults_MatchConstants()
    {
        // Arrange & Act
        var request = new CursorPagedRequest();

        // Assert
        Assert.IsNull(request.Cursor);
        Assert.AreEqual(CursorPagedRequest.DefaultPageSize, request.PageSize);
    }

    [TestMethod]
    public void Request_ImplementsPagedRequestMarker()
    {
        // Arrange & Act
        var request = new CursorPagedRequest();

        // Assert
        Assert.IsInstanceOfType<IPagedRequest>(request);
    }

    [TestMethod]
    public void CursorAndPageSize_AreRetained()
    {
        // Arrange & Act
        var request = new CursorPagedRequest { Cursor = "abc", PageSize = 50 };

        // Assert
        Assert.AreEqual("abc", request.Cursor);
        Assert.AreEqual(50, request.PageSize);
    }
}
