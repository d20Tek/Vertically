namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class SortedFilteredCursorPagedRequestValidatorTests
{
    private readonly SortedFilteredCursorPagedRequestValidator _validator = new();

    [TestMethod]
    public void Validate_ValidRequest_HasNoErrors()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest
        {
            PageSize = 20,
            Sorts = [new SortExpression("Name")],
            Filter = FilterGroup.All(new FilterExpression("Age", FilterOperator.GreaterThan, 21)),
        };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_InheritsBaseCursorBounds()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest { PageSize = 0 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_SortWithEmptyField_HasError()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest
        {
            Sorts = [new SortExpression("  ")],
        };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_FilterWithEmptyField_HasError()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest
        {
            Filter = FilterGroup.All(new FilterExpression("", FilterOperator.Equals, "x")),
        };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_NestedFilterWithEmptyField_HasError()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest
        {
            Filter = FilterGroup.Any(
                new FilterExpression("Status", FilterOperator.Equals, "Active"),
                FilterGroup.All(new FilterExpression("  ", FilterOperator.Equals, "x"))),
        };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_EmptySortsAndNullFilter_HasNoErrors()
    {
        // Arrange
        var request = new SortedFilteredCursorPagedRequest { PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }
}
