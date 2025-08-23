namespace Configuration
{
    public class ExternalApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string OrganizationApi { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 10;
    }
}