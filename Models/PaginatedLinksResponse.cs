namespace KiirLink.Models;

public sealed class PaginatedLinksResponse
{
    public List<LinkModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public int GetLastPage(int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        var calculatedTotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);
        return Math.Max(1, TotalPages > 0 ? TotalPages : calculatedTotalPages);
    }
}
