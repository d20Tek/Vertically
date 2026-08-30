namespace D20Tek.Vertically.Tests.Queries;

[TestClass]
public sealed class SortedFilteredPagedRequestTests
{
    [TestMethod]
    public void Defaults_HaveEmptySortsAndFilters()
    {
        // Arrange & Act
        var request = new SortedFilteredPagedRequest();

        // Assert
        Assert.AreEqual(0, request.Sorts.Count);
        Assert.AreEqual(0, request.Filters.Count);
    }

    [TestMethod]
    public void Request_IsAlsoAPagedRequest()
    {
        // Arrange & Act
        var request = new SortedFilteredPagedRequest();

        // Assert
        Assert.IsInstanceOfType<PagedRequest>(request);
        Assert.IsInstanceOfType<IPagedRequest>(request);
    }

    [TestMethod]
    public void SortsAndFilters_AreRetained()
    {
        // Arrange & Act
        var request = new SortedFilteredPagedRequest
        {
            Sorts = [new SortExpression("Name", SortDirection.Descending)],
            Filters = [new FilterExpression("Age", FilterOperator.GreaterThan, 21)],
        };

        // Assert
        Assert.AreEqual(1, request.Sorts.Count);
        Assert.AreEqual("Name", request.Sorts[0].Field);
        Assert.AreEqual(SortDirection.Descending, request.Sorts[0].Direction);
        Assert.AreEqual(1, request.Filters.Count);
        Assert.AreEqual("Age", request.Filters[0].Field);
        Assert.AreEqual(FilterOperator.GreaterThan, request.Filters[0].Operator);
        Assert.AreEqual(21, request.Filters[0].Value);
    }

    [TestMethod]
    public void SortExpression_DefaultsToAscending()
    {
        // Arrange & Act
        var sort = new SortExpression("Name");

        // Assert
        Assert.AreEqual(SortDirection.Ascending, sort.Direction);
    }
}
