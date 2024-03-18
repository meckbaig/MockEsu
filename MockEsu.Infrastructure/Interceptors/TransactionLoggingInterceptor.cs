using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockEsu.Domain.Common;
using MockEsu.Application.Common.Extensions.Caching;

namespace MockEsu.Infrastructure.Interceptors;

public class TransactionLoggingInterceptor : DbTransactionInterceptor
{
    private readonly ILogger<TransactionLoggingInterceptor> _logger;
    private readonly IDistributedCache _cache;

    public TransactionLoggingInterceptor(ILogger<TransactionLoggingInterceptor> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
    {
        _logger.Log(LogLevel.Information, "Transaction started");
        return base.TransactionStarted(connection, eventData, result);
    }

    public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Information, "Transaction started");
        return base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        _logger.Log(LogLevel.Information, "Transaction commited");
        RemoveChangedEntitiesFromCache(eventData.Context.ChangeTracker);
        base.TransactionCommitted(transaction, eventData);
    }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Information, "Transaction commited");
        RemoveChangedEntitiesFromCache(eventData.Context.ChangeTracker);
        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
    {
        _logger.Log(LogLevel.Error, "Transaction failed");
        base.TransactionFailed(transaction, eventData);
    }

    public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Error, "Transaction failed");
        return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        _logger.Log(LogLevel.Information, "Transaction rolled back");
        base.TransactionRolledBack(transaction, eventData);
    }

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Information, "Transaction rolled back");
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    private void RemoveChangedEntitiesFromCache(ChangeTracker tracker)
    {
        foreach (var entry in tracker.Entries<BaseEntity>())
        {
            if (CachedKeys2.TryGetAndRemoveKeyById(entry.Entity.GetType(), entry.Entity.Id, out var key))
            {
                _cache.RemoveFromCache(key);
            }
        }
    }
}
