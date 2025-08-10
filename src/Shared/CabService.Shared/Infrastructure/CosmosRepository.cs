using Microsoft.Azure.Cosmos;
using CabService.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CabService.Shared.Infrastructure;

public class CosmosRepository<T> : ICosmosRepository<T> where T : BaseEntity
{
    private readonly Container _container;
    private readonly ILogger<CosmosRepository<T>> _logger;

    public CosmosRepository(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CosmosRepository<T>> logger)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "CabServiceDb";
        var containerName = configuration[$"CosmosDb:Containers:{typeof(T).Name}"] ?? typeof(T).Name.ToLower();
        
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item with ID {Id} from container {Container}", id, _container.Id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.isDeleted = false");
            var iterator = _container.GetItemQueryIterator<T>(query, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey)
            });

            var results = new List<T>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all items from container {Container}", _container.Id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync(string query, object parameters, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryDefinition = new QueryDefinition(query);
            
            if (parameters != null)
            {
                var properties = parameters.GetType().GetProperties();
                foreach (var property in properties)
                {
                    queryDefinition.WithParameter($"@{property.Name}", property.GetValue(parameters));
                }
            }

            var iterator = _container.GetItemQueryIterator<T>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey)
            });

            var results = new List<T>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query on container {Container}: {Query}", _container.Id, query);
            throw;
        }
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            
            var response = await _container.CreateItemAsync(entity, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Created item with ID {Id} in container {Container}", entity.Id, _container.Id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create item with ID {Id} in container {Container}", entity.Id, _container.Id);
            throw;
        }
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            entity.UpdatedAt = DateTime.UtcNow;
            
            var response = await _container.ReplaceItemAsync(entity, entity.Id, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Updated item with ID {Id} in container {Container}", entity.Id, _container.Id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update item with ID {Id} in container {Container}", entity.Id, _container.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            // Soft delete - mark as deleted instead of physically removing
            var item = await GetByIdAsync(id, partitionKey, cancellationToken);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.UtcNow;
                await UpdateAsync(item, cancellationToken);
            }
            
            _logger.LogInformation("Soft deleted item with ID {Id} in container {Container}", id, _container.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete item with ID {Id} in container {Container}", id, _container.Id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await GetByIdAsync(id, partitionKey, cancellationToken);
            return item != null && !item.IsDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of item with ID {Id} in container {Container}", id, _container.Id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetPagedAsync(string continuationToken, int pageSize, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.isDeleted = false ORDER BY c.createdAt DESC");
            var iterator = _container.GetItemQueryIterator<T>(query, continuationToken, new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey),
                MaxItemCount = pageSize
            });

            var results = new List<T>();
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paged items from container {Container}", _container.Id);
            throw;
        }
    }
}
