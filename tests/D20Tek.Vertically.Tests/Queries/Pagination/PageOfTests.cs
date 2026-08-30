namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class PageOfTests
{
    [TestMethod]
    public void Defaults_AreEmpty()
    {
        // Arrange & Act
        var page = new PageOf<string>();

        // Assert
        Assert.AreEqual(0, page.Items.Count);
        Assert.AreEqual(0, page.PageNumber);
        Assert.AreEqual(0, page.PageSize);
        Assert.AreEqual(0L, page.TotalCount);
    }

    [TestMethod]
    [DataRow(0, 20, 0)]
    [DataRow(20, 20, 1)]
    [DataRow(21, 20, 2)]
    [DataRow(100, 20, 5)]
    [DataRow(101, 20, 6)]
    public void TotalPages_ComputesCeiling(long totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var page = new PageOf<string> { PageSize = pageSize, TotalCount = totalCount };

        // Act & Assert
        Assert.AreEqual(expectedPages, page.TotalPages);
    }

    [TestMethod]
    public void TotalPages_WithZeroPageSize_IsZero()
    {
        // Arrange
        var page = new PageOf<string> { PageSize = 0, TotalCount = 42 };

        // Act & Assert
        Assert.AreEqual(0, page.TotalPages);
    }

    [TestMethod]
    [DataRow(1, false)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    public void HasPrevious_TrueWhenNotFirstPage(int pageNumber, bool expected)
    {
        // Arrange
        var page = new PageOf<string> { PageNumber = pageNumber, PageSize = 10, TotalCount = 100 };

        // Act & Assert
        Assert.AreEqual(expected, page.HasPrevious);
    }

    [TestMethod]
    [DataRow(1, true)]
    [DataRow(9, true)]
    [DataRow(10, false)]
    public void HasNext_TrueWhenBeforeLastPage(int pageNumber, bool expected)
    {
        // Arrange
        var page = new PageOf<string> { PageNumber = pageNumber, PageSize = 10, TotalCount = 100 };

        // Act & Assert
        Assert.AreEqual(expected, page.HasNext);
    }

    [TestMethod]
    public void Create_DerivesMetadataFromRequest()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 2, PageSize = 25 };
        IReadOnlyList<string> items = ["a", "b", "c"];

        // Act
        var page = PageOf<string>.Create(items, request, totalCount: 123);

        // Assert
        Assert.AreSame(items, page.Items);
        Assert.AreEqual(2, page.PageNumber);
        Assert.AreEqual(25, page.PageSize);
        Assert.AreEqual(123L, page.TotalCount);
        Assert.AreEqual(5, page.TotalPages);
        Assert.IsTrue(page.HasPrevious);
        Assert.IsTrue(page.HasNext);
    }

    [TestMethod]
    public void Empty_PreservesRequestMetadataWithNoItems()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 3, PageSize = 25 };

        // Act
        var page = PageOf<string>.Empty(request);

        // Assert
        Assert.AreEqual(0, page.Items.Count);
        Assert.AreEqual(3, page.PageNumber);
        Assert.AreEqual(25, page.PageSize);
        Assert.AreEqual(0L, page.TotalCount);
        Assert.AreEqual(0, page.TotalPages);
    }

    [TestMethod]
    public void Map_ProjectsItemsAndPreservesMetadata()
    {
        // Arrange
        var request = new PagedRequest { PageNumber = 2, PageSize = 25 };
        var page = PageOf<int>.Create([1, 2, 3], request, totalCount: 123);

        // Act
        var mapped = page.Map(i => i.ToString());

        // Assert
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, mapped.Items.ToArray());
        Assert.AreEqual(2, mapped.PageNumber);
        Assert.AreEqual(25, mapped.PageSize);
        Assert.AreEqual(123L, mapped.TotalCount);
        Assert.AreEqual(5, mapped.TotalPages);
    }

    [TestMethod]
    public void Map_NullSelector_Throws()
    {
        // Arrange
        var page = PageOf<int>.Create([1], new PagedRequest(), totalCount: 1);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>([ExcludeFromCodeCoverage]() => page.Map<string>(null!));
    }
}
