using Common.Helpers;

namespace UnitTests.Common.Helpers;

public class CommentsHelperTests
{
    [Fact]
    public void CountComments_ReturnsZero_WhenNull()
    {
        Assert.Equal(0, CommentsHelper.CountComments(null!));
    }

    [Fact]
    public void CountComments_ReturnsZero_WhenEmpty()
    {
        Assert.Equal(0, CommentsHelper.CountComments(new List<int>()));
    }

    [Fact]
    public void CountComments_ReturnsCount()
    {
        Assert.Equal(3, CommentsHelper.CountComments(new List<int> { 1, 2, 3 }));
    }
}
