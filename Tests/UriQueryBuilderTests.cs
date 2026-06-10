using KiirLink.Services;

namespace KiirLink.Tests;

public sealed class UriQueryBuilderTests
{
    [Fact]
    public void Build_EncodesValuesAndSkipsNulls()
    {
        var result = UriQueryBuilder.Build(
            "/api/links/shorten",
            ("originalUrl", "https://example.com/a path?q=1"),
            ("isPublic", true),
            ("expiresAt", null),
            ("categoryId", 9));

        Assert.Equal(
            "/api/links/shorten?originalUrl=https%3A%2F%2Fexample.com%2Fa%20path%3Fq%3D1&isPublic=true&categoryId=9",
            result);
    }

    [Fact]
    public void Build_ReturnsPathWhenNoValuesExist()
    {
        Assert.Equal("/api/links/1/category",
            UriQueryBuilder.Build("/api/links/1/category", ("categoryId", null)));
    }
}
