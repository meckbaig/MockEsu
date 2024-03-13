using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Common;
using Nest;

namespace MockEsu.Infrastructure.Data;

public class ElasticSearchClient : IElasticSearchClient
{
    private readonly IElasticClient _client;

    public ElasticSearchClient(IElasticClient client)
    {
        _client = client;
    }

    public async Task SearchAsync<TEntity>(string searchString)
        where TEntity : BaseEntity
    {
        var results = await _client.SearchAsync<TEntity>(
            s => s.Query(
                q => q.QueryString(
                    d => d.Query($"*{string.Join('*', searchString.Replace("  ", " ").Split(' '))}*")
                    )
                )
            );

    }
}
