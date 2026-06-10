using KiirLink.Models;

namespace KiirLink.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void GetLastPage_CalculatesPageFromTotalCount(int totalCount, int pageSize, int expected)
    {
        var response = new PaginatedLinksResponse { TotalCount = totalCount };

        Assert.Equal(expected, response.GetLastPage(pageSize));
    }

    [Fact]
    public void GetLastPage_UsesServerTotalPagesWhenAvailable()
    {
        var response = new PaginatedLinksResponse
        {
            TotalCount = 25,
            TotalPages = 4
        };

        Assert.Equal(4, response.GetLastPage(10));
    }
}
