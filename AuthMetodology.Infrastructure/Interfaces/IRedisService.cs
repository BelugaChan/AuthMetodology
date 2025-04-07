using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Infrastructure.Interfaces
{
    public interface IRedisService
    {
        Task<T> GetStringFromCacheAsync<T>(string key);

        Task SetStringToCacheAsync<T>(string key, T value);

        Task RemoveStringFromCacheAsync(string key);
    }
}
