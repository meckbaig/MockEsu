using MockEsu.Domain.Common;

namespace MockEsu.Application.Common.Interfaces;

public interface IElasticSearchClient
{
    Task<List<TEntity>> SearchAsync<TEntity>(string searchString) where TEntity : class;
    Task AddAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
}
