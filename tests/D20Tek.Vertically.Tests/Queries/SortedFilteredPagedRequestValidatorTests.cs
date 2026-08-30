namespace D20Tek.Vertically.Tests.Queries;

[TestClass]
public sealed class SortedFilteredPagedRequestValidatorTests
{
    private readonly SortedFilteredPagedRequestValidator _validator = new();

    [TestMethod]
    public void Validate_ValidRequest_HasNoErrors()
    {
        // Arrange
        var request = new SortedFilteredPagedRequest
        {
            PageNumber = 1,
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
    public void Validate_InheritsBasePagingBounds()
    {
        // Arrange
        var request = new SortedFilteredPagedRequest { PageNumber = 0, PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_SortWithEmptyField_HasError()
    {
        // Arrange
        var request = new SortedFilteredPagedRequest
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
        var request = new SortedFilteredPagedRequest
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
        var request = new SortedFilteredPagedRequest
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
        var request = new SortedFilteredPagedRequest { PageNumber = 1, PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }
}
