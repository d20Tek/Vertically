namespace D20Tek.Vertically.Tests.Queries.Pagination;

[TestClass]
public sealed class CursorPageOfTests
{
    [TestMethod]
    public void Defaults_AreEmpty()
    {
        // Arrange & Act
        var page = new CursorPageOf<string>();

        // Assert
        Assert.AreEqual(0, page.Items.Count);
        Assert.AreEqual(0, page.PageSize);
        Assert.IsNull(page.NextCursor);
        Assert.IsNull(page.PreviousCursor);
        Assert.IsFalse(page.HasNext);
        Assert.IsFalse(page.HasPrevious);
    }

    [TestMethod]
    public void HasNext_TrueWhenNextCursorPresent()
    {
        // Arrange
        var page = new CursorPageOf<string> { NextCursor = "n" };

        // Act & Assert
        Assert.IsTrue(page.HasNext);
        Assert.IsFalse(page.HasPrevious);
    }

    [TestMethod]
    public void HasPrevious_TrueWhenPreviousCursorPresent()
    {
        // Arrange
        var page = new CursorPageOf<string> { PreviousCursor = "p" };

        // Act & Assert
        Assert.IsTrue(page.HasPrevious);
        Assert.IsFalse(page.HasNext);
    }

    [TestMethod]
    public void Create_DerivesPageSizeAndCursors()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = 25 };
        IReadOnlyList<string> items = ["a", "b"];

        // Act
        var page = CursorPageOf<string>.Create(items, request, nextCursor: "n", previousCursor: "p");

        // Assert
        Assert.AreSame(items, page.Items);
        Assert.AreEqual(25, page.PageSize);
        Assert.AreEqual("n", page.NextCursor);
        Assert.AreEqual("p", page.PreviousCursor);
        Assert.IsTrue(page.HasNext);
        Assert.IsTrue(page.HasPrevious);
    }

    [TestMethod]
    public void Empty_PreservesPageSizeWithNoItemsOrCursors()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = 25 };

        // Act
        var page = CursorPageOf<string>.Empty(request);

        // Assert
        Assert.AreEqual(0, page.Items.Count);
        Assert.AreEqual(25, page.PageSize);
        Assert.IsNull(page.NextCursor);
        Assert.IsNull(page.PreviousCursor);
        Assert.IsFalse(page.HasNext);
        Assert.IsFalse(page.HasPrevious);
    }

    [TestMethod]
    public void Map_ProjectsItemsAndPreservesCursors()
    {
        // Arrange
        var request = new CursorPagedRequest { PageSize = 25 };
        var page = CursorPageOf<int>.Create([1, 2, 3], request, nextCursor: "n", previousCursor: "p");

        // Act
        var mapped = page.Map(i => i.ToString());

        // Assert
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, mapped.Items.ToArray());
        Assert.AreEqual(25, mapped.PageSize);
        Assert.AreEqual("n", mapped.NextCursor);
        Assert.AreEqual("p", mapped.PreviousCursor);
    }

    [TestMethod]
    public void Map_NullSelector_Throws()
    {
        // Arrange
        var page = CursorPageOf<int>.Create([1], new CursorPagedRequest(), nextCursor: null);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>([ExcludeFromCodeCoverage] () => page.Map<string>(null!));
    }
}
