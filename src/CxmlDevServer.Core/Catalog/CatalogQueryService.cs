using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Catalog;

/// <summary>How catalog search results are ordered.</summary>
public enum CatalogSort
{
    /// <summary>By short name.</summary>
    Name,

    /// <summary>By unit price, cheapest first.</summary>
    PriceAscending,

    /// <summary>By unit price, most expensive first.</summary>
    PriceDescending,
}

/// <summary>A catalog search.</summary>
/// <param name="Text">Words to look for in name, description, part numbers and manufacturer (case-insensitive).</param>
/// <param name="Category">Restrict to one category.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="Sort">Ordering.</param>
/// <param name="IncludeInactive">Include items hidden from the shop (for the admin catalog).</param>
public sealed record CatalogQuery(
    string? Text = null,
    string? Category = null,
    int Page = 1,
    int PageSize = 12,
    CatalogSort Sort = CatalogSort.Name,
    bool IncludeInactive = false);

/// <summary>One page of results.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on the page.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="TotalCount">Total matching items.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when a following page exists.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>True when a previous page exists.</summary>
    public bool HasPrevious => Page > 1;
}

/// <summary>A category with its number of items.</summary>
/// <param name="Name">Category name.</param>
/// <param name="ItemCount">Number of (active) items.</param>
public sealed record CategorySummary(string Name, int ItemCount);

/// <summary>Searches the catalog for the PunchOut shop and the admin pages.</summary>
public sealed class CatalogQueryService
{
    private readonly ICxmlDbContext _db;

    /// <summary>Creates the service.</summary>
    public CatalogQueryService(ICxmlDbContext db) => _db = db;

    /// <summary>Runs a search.</summary>
    public async Task<PagedResult<CatalogItem>> SearchAsync(CatalogQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = _db.CatalogItems.AsNoTracking();
        if (!query.IncludeInactive)
        {
            items = items.Where(i => i.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            items = items.Where(i => i.Category == category);
        }

        foreach (var word in (query.Text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pattern = "%" + word.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            items = items.Where(i =>
                EF.Functions.Like(i.ShortName, pattern, "\\")
                || EF.Functions.Like(i.Description, pattern, "\\")
                || EF.Functions.Like(i.SupplierPartId, pattern, "\\")
                || (i.ManufacturerName != null && EF.Functions.Like(i.ManufacturerName, pattern, "\\"))
                || (i.ManufacturerPartId != null && EF.Functions.Like(i.ManufacturerPartId, pattern, "\\")));
        }

        var total = await items.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            CatalogSort.PriceAscending => items.OrderBy(i => i.UnitPrice).ThenBy(i => i.ShortName),
            CatalogSort.PriceDescending => items.OrderByDescending(i => i.UnitPrice).ThenBy(i => i.ShortName),
            _ => items.OrderBy(i => i.ShortName).ThenBy(i => i.Id),
        };
        var page1 = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<CatalogItem>(page1, page, pageSize, total);
    }

    /// <summary>Lists the categories of active items.</summary>
    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await _db.CatalogItems.AsNoTracking()
            .Where(i => i.IsActive)
            .GroupBy(i => i.Category)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        .Select(c => new CategorySummary(c.Name, c.Count))
        .ToList();

    /// <summary>Finds one item.</summary>
    public Task<CatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.CatalogItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
}
