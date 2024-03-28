using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MockEsu.Infrastructure.Caching.AsyncSqlCachedKeysProvider;

namespace MockEsu.Infrastructure.AsyncMessaging;

internal class CachedKeyDataMessage
{
    public CachedKeyData CachedKey { get; set; }
}
