namespace KiirLink.Models;

public sealed class PaginatedLinksResponse
{
    public List<LinkModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
