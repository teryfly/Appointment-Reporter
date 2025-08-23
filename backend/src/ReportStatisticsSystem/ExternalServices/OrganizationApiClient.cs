using Configuration;
using ExternalServices.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models.Entities;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ExternalServices
{
    public class OrganizationApiClient : IOrganizationApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ExternalApiConfig _apiConfig;
        private readonly ILogger<OrganizationApiClient> _logger;

        public OrganizationApiClient(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<ExternalApiConfig> apiConfig,
            ILogger<OrganizationApiClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(OrganizationApiClient));
            _cache = cache;
            _apiConfig = apiConfig.Value;
            _logger = logger;
        }

        public async Task<List<OrganizationEntity>> GetOrganizationsAsync(string sceneCode)
        {
            var cacheKey = CacheKeyGenerator.OrganizationKey(sceneCode);
            if (_cache.TryGetValue(cacheKey, out List<OrganizationEntity> orgs))
            {
                return orgs;
            }

            var url = $"{_apiConfig.BaseUrl}{_apiConfig.OrganizationApi}{sceneCode}";
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(_apiConfig.TimeoutSeconds));
                var resp = await _httpClient.GetAsync(url, cts.Token);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch organizations for scene {sceneCode}, status: {statusCode}", sceneCode, resp.StatusCode);
                    return new List<OrganizationEntity>();
                }

                var organizations = await resp.Content.ReadFromJsonAsync<List<OrganizationEntity>>(cancellationToken: cts.Token)
                    ?? new List<OrganizationEntity>();

                _cache.Set(cacheKey, organizations, TimeSpan.FromMinutes(60));
                return organizations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while calling organization API for scene {sceneCode}", sceneCode);
                return new List<OrganizationEntity>();
            }
        }
    }
}