using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Services.Interfaces;

namespace Services.Fhir
{
    internal class FhirPatientService : IPatientService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FhirPatientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GetPatientNameAsync(string patientId)
        {
            if (string.IsNullOrWhiteSpace(patientId))
                return "未知";

            try
            {
                using var resp = await _http.GetAsync($"Patient/{Uri.EscapeDataString(patientId)}");
                if (!resp.IsSuccessStatusCode)
                    return "未知";

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var p = await JsonSerializer.DeserializeAsync<FhirPatient>(stream, JsonOpts);
                var name = p?.Name != null && p.Name.Count > 0
                    ? (p.Name[0].Text ?? "").Trim()
                    : "";
                return string.IsNullOrWhiteSpace(name) ? "未知" : name;
            }
            catch
            {
                return "未知";
            }
        }

        private class FhirPatient
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("name")]
            public System.Collections.Generic.List<HumanName>? Name { get; set; }
        }

        private class HumanName
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}