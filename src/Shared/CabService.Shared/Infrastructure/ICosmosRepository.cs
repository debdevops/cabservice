using CabService.Shared.Models;

namespace CabService.Shared.Infrastructure;

public interface ICosmosRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(string partitionKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryAsync(string query, object parameters, string partitionKey, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetPagedAsync(string continuationToken, int pageSize, string partitionKey, CancellationToken cancellationToken = default);
}
