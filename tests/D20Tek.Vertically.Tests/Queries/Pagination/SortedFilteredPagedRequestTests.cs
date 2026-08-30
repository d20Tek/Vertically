namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class SortedFilteredPagedRequestTests
{
    [TestMethod]
    public void Defaults_HaveEmptySortsAndNullFilter()
    {
        // Arrange & Act
        var request = new SortedFilteredPagedRequest();

        // Assert
        Assert.AreEqual(0, request.Sorts.Count);
        Assert.IsNull(request.Filter);
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
    public void SortsAndFilter_AreRetained()
    {
        // Arrange & Act
        var request = new SortedFilteredPagedRequest
        {
            Sorts = [new SortExpression("Name", SortDirection.Descending)],
            Filter = FilterGroup.All(new FilterExpression("Age", FilterOperator.GreaterThan, 21)),
        };

        // Assert
        Assert.AreEqual(1, request.Sorts.Count);
        Assert.AreEqual("Name", request.Sorts[0].Field);
        Assert.AreEqual(SortDirection.Descending, request.Sorts[0].Direction);
        Assert.IsNotNull(request.Filter);
        Assert.AreEqual(FilterLogic.And, request.Filter.Logic);
        Assert.AreEqual(1, request.Filter.Nodes.Count);
        var leaf = (FilterExpression)request.Filter.Nodes[0];
        Assert.AreEqual("Age", leaf.Field);
        Assert.AreEqual(FilterOperator.GreaterThan, leaf.Operator);
        Assert.AreEqual(21, leaf.Value);
    }

    [TestMethod]
    public void FilterGroup_SupportsOrAndNesting()
    {
        // Arrange & Act
        var filter = FilterGroup.Any(
            new FilterExpression("Status", FilterOperator.Equals, "Active"),
            FilterGroup.All(
                new FilterExpression("Age", FilterOperator.GreaterThanOrEqual, 18),
                new FilterExpression("Age", FilterOperator.LessThan, 65)));

        // Assert
        Assert.AreEqual(FilterLogic.Or, filter.Logic);
        Assert.AreEqual(2, filter.Nodes.Count);
        Assert.IsInstanceOfType<FilterExpression>(filter.Nodes[0]);
        var nested = (FilterGroup)filter.Nodes[1];
        Assert.AreEqual(FilterLogic.And, nested.Logic);
        Assert.AreEqual(2, nested.Nodes.Count);
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
