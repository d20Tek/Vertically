namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class SortedFilteredCursorPagedRequestTests
{
    [TestMethod]
    public void Defaults_HaveEmptySortsAndNullFilter()
    {
        // Arrange & Act
        var request = new SortedFilteredCursorPagedRequest();

        // Assert
        Assert.AreEqual(0, request.Sorts.Count);
        Assert.IsNull(request.Filter);
    }

    [TestMethod]
    public void Request_IsAlsoACursorPagedRequest()
    {
        // Arrange & Act
        var request = new SortedFilteredCursorPagedRequest();

        // Assert
        Assert.IsInstanceOfType<CursorPagedRequest>(request);
        Assert.IsInstanceOfType<IPagedRequest>(request);
    }

    [TestMethod]
    public void CursorSortsAndFilter_AreRetained()
    {
        // Arrange & Act
        var request = new SortedFilteredCursorPagedRequest
        {
            Cursor = "abc",
            PageSize = 50,
            Sorts = [new SortExpression("Name", SortDirection.Descending)],
            Filter = FilterGroup.All(new FilterExpression("Age", FilterOperator.GreaterThan, 21)),
        };

        // Assert
        Assert.AreEqual("abc", request.Cursor);
        Assert.AreEqual(50, request.PageSize);
        Assert.AreEqual(1, request.Sorts.Count);
        Assert.AreEqual("Name", request.Sorts[0].Field);
        Assert.IsNotNull(request.Filter);
        Assert.AreEqual(FilterLogic.And, request.Filter.Logic);
        Assert.AreEqual(1, request.Filter.Nodes.Count);
    }
}
