using MockEsu.Domain.Common;

namespace MockEsu.Application.Common.Interfaces;

public interface IElasticSearchClient
{
    Task SearchAsync<TEntity>(string searchString) where TEntity : BaseEntity;
}
