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

    public async Task<List<TEntity>> SearchAsync<TEntity>(string searchString)
        where TEntity : class
    {
        var result = await _client.SearchAsync<TEntity>(
            s => s.Query(
                q => q.MultiMatch(
                    d => d.Query(ReplaceSpacesWithStars(searchString))
                    )
                ).Size(50)
            );
        return result.Documents.ToList();
    }

    private static string ReplaceSpacesWithStars(string input)
    {
        return string.Join('*', $" {input} ".Replace("  ", " ").Split(' '));
    }

    public async Task AddAsync<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var response = await _client.IndexManyAsync(entities);
    }
}
