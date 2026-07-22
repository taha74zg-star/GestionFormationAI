using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Infrastructure.Services;

public sealed class AdminCatalogService(ApplicationDbContext db) : IAdminCatalogService
{
    public async Task<PagedResult<Categorie>> GetCategoriesAsync(string? search, string? sort, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Categories.Include(c => c.Parent).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Nom.Contains(search));
        query = sort == "name_desc" ? query.OrderByDescending(c => c.Nom) : sort == "order" ? query.OrderBy(c => c.Ordre).ThenBy(c => c.Nom) : query.OrderBy(c => c.Nom);
        return await PageAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<FormateurSummary>> GetFormateursAsync(string? search, string? sort, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Formateurs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(f => f.Nom.Contains(search) || f.Prenom.Contains(search) || (f.Specialites ?? "").Contains(search));
        query = sort == "name_desc" ? query.OrderByDescending(f => f.Nom) : query.OrderBy(f => f.Nom).ThenBy(f => f.Prenom);
        var count = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(f => new FormateurSummary(f, f.Formations.Count(x => x.EstPubliee))).ToListAsync(ct);
        return new PagedResult<FormateurSummary>(items, page, pageSize, count);
    }

    public async Task<(bool Deleted, string? Error)> DeleteCategorieAsync(int id, CancellationToken ct = default)
    {
        var item = await db.Categories.FindAsync([id], ct);
        if (item is null) return (false, "Catégorie introuvable.");
        if (await db.Categories.AnyAsync(c => c.ParentId == id, ct)) return (false, "Cette catégorie contient des sous-catégories.");
        db.Categories.Remove(item); await db.SaveChangesAsync(ct); return (true, null);
    }

    public async Task<(bool Deleted, string? Error)> DeleteFormateurAsync(int id, CancellationToken ct = default)
    {
        var item = await db.Formateurs.Include(f => f.Formations).SingleOrDefaultAsync(f => f.Id == id, ct);
        if (item is null) return (false, "Formateur introuvable.");
        if (item.Formations.Any(f => f.EstPubliee)) return (false, "Ce formateur a des formations actives. Désactivez-les ou réassignez-les avant suppression.");
        db.Formateurs.Remove(item); await db.SaveChangesAsync(ct); return (true, null);
    }

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 20);
        var count = await query.CountAsync(ct); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, count);
    }
}
