namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [TestMethod]
    public void Validate_ValidRequest_HasNoErrors()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 1, PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageNumberBelowOne_HasError()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 0, PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeBelowOne_HasError()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 1, PageSize = 0 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeExceedsMax_HasError()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 1, PageSize = PagedRequest.MaxPageSize + 1 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeAtMax_HasNoErrors()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 1, PageSize = PagedRequest.MaxPageSize };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_IsContravariant_AcceptsSubclass()
    {
        // Arrange
        IValidator<PagedRequest> validator = _validator;
        var request = new SortedFilteredPagedRequest { PageNumber = 1, PageSize = 20 };

        // Act
        var errors = validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }
}
