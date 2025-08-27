using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Services.Interfaces;

namespace Services.Fhir
{
    internal class FhirLookupService : IFhirLookupService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FhirLookupService(HttpClient http)
        {
            _http = http;
        }

        public async Task<Dictionary<string, string>> GetOrganizationNamesAsync(IEnumerable<string> orgIds)
        {
            var result = new Dictionary<string, string>();
            var ids = orgIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            foreach (var id in ids)
            {
                var name = await GetOrganizationNameAsync(id);
                if (!result.ContainsKey(id))
                    result[id] = name ?? "";
            }
            return result;
        }

        public async Task<Dictionary<string, string>> GetPractitionerNamesAsync(IEnumerable<string> practitionerIds)
        {
            var result = new Dictionary<string, string>();
            var ids = practitionerIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            foreach (var id in ids)
            {
                var name = await GetPractitionerNameAsync(id);
                if (!result.ContainsKey(id))
                    result[id] = name ?? "";
            }
            return result;
        }

        private async Task<string?> GetOrganizationNameAsync(string id)
        {
            try
            {
                using var resp = await _http.GetAsync($"Organization/{Uri.EscapeDataString(id)}");
                if (!resp.IsSuccessStatusCode) return null;
                await using var stream = await resp.Content.ReadAsStreamAsync();
                var org = await JsonSerializer.DeserializeAsync<FhirOrganization>(stream, JsonOpts);
                return org?.Name ?? "";
            }
            catch { return null; }
        }

        private async Task<string?> GetPractitionerNameAsync(string id)
        {
            try
            {
                using var resp = await _http.GetAsync($"Practitioner/{Uri.EscapeDataString(id)}");
                if (!resp.IsSuccessStatusCode) return null;
                await using var stream = await resp.Content.ReadAsStreamAsync();
                var p = await JsonSerializer.DeserializeAsync<FhirPractitioner>(stream, JsonOpts);
                if (p?.Name != null && p.Name.Count > 0)
                {
                    var n = p.Name[0];
                    if (!string.IsNullOrWhiteSpace(n.Text)) return n.Text!.Trim();
                    var family = n.Family ?? "";
                    var given = n.Given != null ? string.Join("", n.Given) : "";
                    var merged = $"{family}{given}".Trim();
                    return merged;
                }
                return "";
            }
            catch { return null; }
        }

        private class FhirOrganization
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        private class FhirPractitioner
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("name")]
            public List<HumanName>? Name { get; set; }
        }

        private class HumanName
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
            [JsonPropertyName("family")]
            public string? Family { get; set; }
            [JsonPropertyName("given")]
            public List<string>? Given { get; set; }
        }
    }
}