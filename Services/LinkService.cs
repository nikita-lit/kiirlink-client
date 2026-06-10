using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// High-level link operations built on top of <see cref="ApiClient"/>.
/// </summary>
public class LinkService(IApiClient api) : ILinkService
{
    public Task<PaginatedLinksResponse> GetLinksPageAsync( int page = 1, int limit = 20,
        int? categoryId = null )
        => api.GetLinksPageAsync( page, limit, categoryId );

    public Task<List<LinkModel>> GetLinksAsync( int page = 1, int limit = 20, int? categoryId = null )
        => api.GetLinksAsync( page, limit, categoryId );

    public Task<(bool Success, string? Error)> ShortenLinkAsync( string originalUrl, DateTime? expiresAt = null,
        bool isPublic = true )
        => api.ShortenLinkAsync( originalUrl, expiresAt, isPublic );

    public Task<bool> RemoveLinkAsync( int linkId )
        => api.RemoveLinkAsync( linkId );

    public Task<LinkStatsModel?> GetLinkStatsAsync( int id )
        => api.GetLinkStatsAsync( id );

    public Task<List<LinkActivityModel>> GetLinkActivityAsync( int id )
        => api.GetLinkActivityAsync( id );

    public Task<List<LinkModel>> GetFavouritesAsync()
        => api.GetFavouritesAsync();

    public Task<bool> AddFavouriteAsync( int linkId )
        => api.AddFavouriteAsync( linkId );

    public Task<bool> RemoveFavouriteAsync( int linkId )
        => api.RemoveFavouriteAsync( linkId );

    public Task<List<CategoryModel>> GetCategoriesAsync()
        => api.GetCategoriesAsync();

    public Task<CategoryModel?> CreateCategoryAsync( string name )
        => api.CreateCategoryAsync( name );

    public Task<bool> AddCategoryAsync( string name )
        => api.AddCategoryAsync( name );

    public Task<bool> DeleteCategoryAsync( int id )
        => api.DeleteCategoryAsync( id );

    public Task<bool> AssignCategoryAsync( int linkId, int? categoryId )
        => api.AssignCategoryAsync( linkId, categoryId );
}
