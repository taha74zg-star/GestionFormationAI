using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Infrastructure.Services;

public class FormationService : IFormationService
{
    private readonly ApplicationDbContext _db;

    public FormationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Formation> CreateAsync(Formation formation, CancellationToken cancellationToken = default)
    {
        _db.Formations.Add(formation);
        await _db.SaveChangesAsync(cancellationToken);
        return formation;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var f = await _db.Formations.FindAsync(new object[] { id }, cancellationToken);
        if (f == null) return false;
        _db.Formations.Remove(f);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Formation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Formations.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Formation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Formations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Formation> UpdateAsync(Formation formation, CancellationToken cancellationToken = default)
    {
        _db.Formations.Update(formation);
        await _db.SaveChangesAsync(cancellationToken);
        return formation;
    }
}
