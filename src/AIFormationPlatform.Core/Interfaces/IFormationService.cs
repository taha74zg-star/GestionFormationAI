using AIFormationPlatform.Core.Entities;

namespace AIFormationPlatform.Core.Interfaces;

public interface IFormationService
{
    Task<Formation> CreateAsync(Formation formation, CancellationToken cancellationToken = default);
    Task<Formation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Formation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Formation> UpdateAsync(Formation formation, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
