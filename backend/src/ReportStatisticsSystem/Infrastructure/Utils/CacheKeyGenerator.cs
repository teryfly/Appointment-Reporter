using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Utils
{
    public static class CacheKeyGenerator
    {
        public static string OrganizationKey(string sceneCode) =>
            $"organizations_{sceneCode}";

        public static string ReportCacheKey(string reportType, IDictionary<string, object> parameters)
        {
            var paramString = string.Join("_", parameters.OrderBy(p => p.Key)
                .Select(p => $"{p.Key}:{p.Value}"));
            return $"report_{reportType}_{paramString}";
        }
    }
}