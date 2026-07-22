using AIFormationPlatform.Core.Entities;

namespace AIFormationPlatform.Core.Interfaces;

public interface IAdminCatalogService
{
    Task<PagedResult<Categorie>> GetCategoriesAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<FormateurSummary>> GetFormateursAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(bool Deleted, string? Error)> DeleteCategorieAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Deleted, string? Error)> DeleteFormateurAsync(int id, CancellationToken cancellationToken = default);
}

public interface IPagedResult { int Page { get; } int TotalPages { get; } }

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) : IPagedResult
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record FormateurSummary(Formateur Formateur, int FormationsActives);
