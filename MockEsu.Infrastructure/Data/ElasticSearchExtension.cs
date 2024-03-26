using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Infrastructure.Data;

internal static class ElasticSearchExtension
{
    public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
    {
        string url = configuration.GetConnectionString("ElasticSearch");
        string defaultIndex = "kontragents";

        var settings = new ConnectionSettings(new Uri(url))
            .PrettyJson()
            .CertificateFingerprint("205b1fbdcdb4755f67bbab0518616e55d80aee5c6a1a729e8605210508364333")
            .BasicAuthentication("elastic", "SLdC*BPfYs_m7h=lgD8B")
            .DefaultIndex(defaultIndex);

        // AddDefaultMappings(settings);
        var client = new ElasticClient(settings);
        services.AddSingleton<IElasticSearchClient>(new ElasticSearchClient(client));
        CreateIndex(client, defaultIndex);
    }

    private static void AddDefaultMappings(ConnectionSettings settings)
    {
        settings.DefaultMappingFor<Kontragent>(k => k
            .Ignore(x => x.Id)
            .Ignore(x => x.AddressId)
            .Ignore(x => x.KontragentAgreement)
            .Ignore(x => x.Address.Id)
            .Ignore(x => x.Address.CityId)
            .Ignore(x => x.Address.StreetId)
            .Ignore(x => x.Address.RegionId)
            .Ignore(x => x.Address.City.Id)
            .Ignore(x => x.Address.Region.Id)
            .Ignore(x => x.Address.Street.Id)
            .Ignore(x => x.Address.Street.CityId)
            .Ignore(x => x.Address.Street.City.Id)
        );
    }

    private static void CreateIndex(IElasticClient client, string indexName)
    {
        client.Indices.Create(indexName, i => i.Map<Kontragent>(x => x.AutoMap()));
    }
}
