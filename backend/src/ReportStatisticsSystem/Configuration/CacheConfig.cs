namespace Configuration
{
    public class CacheConfig
    {
        public string Provider { get; set; } = "Memory";
        public string? RedisConnectionString { get; set; }
        public int OrganizationCacheMinutes { get; set; } = 60;
    }
}