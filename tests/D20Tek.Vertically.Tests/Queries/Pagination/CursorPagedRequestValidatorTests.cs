namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class CursorPagedRequestValidatorTests
{
    private readonly CursorPagedRequestValidator _validator = new();

    [TestMethod]
    public void Validate_ValidRequest_HasNoErrors()
    {
        // Arrange
        var request = new CursorPagedRequest { Cursor = "abc", PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_NullCursor_HasNoErrors()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = 20 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeBelowOne_HasError()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = 0 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeExceedsMax_HasError()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = CursorPagedRequest.MaxPageSize + 1 };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsTrue(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_PageSizeAtMax_HasNoErrors()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = CursorPagedRequest.MaxPageSize };

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }

    [TestMethod]
    public void Validate_IsContravariant_AcceptsSubclass()
    {
        // Arrange
        IValidator<CursorPagedRequest> validator = _validator;
        var request = new SortedFilteredCursorPagedRequest { PageSize = 20 };

        // Act
        var errors = validator.Validate(request);

        // Assert
        Assert.IsFalse(errors.HasErrors);
    }
}
