using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.Interfaces;

public interface ICachedKeysProvider
{
    Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id);
    Task<bool> TryCompleteFormationAsync(string key);
    Task<List<string>> GetAndRemoveKeysByIdAsync(Type entityType, int id);
}
