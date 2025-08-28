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
    internal class FhirServiceRequestService : IFhirServiceRequestService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FhirServiceRequestService(HttpClient http)
        {
            _http = http;
        }

        public async Task<Dictionary<ServiceRequestStatKey, int>> CountOrdersAsync(ServiceRequestFilter filter)
        {
            return await QueryAndGroupAsync(filter, includeIntentFillerOrder: false);
        }

        public async Task<Dictionary<ServiceRequestStatKey, int>> CountAppointmentsAsync(ServiceRequestFilter filter)
        {
            return await QueryAndGroupAsync(filter, includeIntentFillerOrder: true);
        }

        private async Task<Dictionary<ServiceRequestStatKey, int>> QueryAndGroupAsync(ServiceRequestFilter filter, bool includeIntentFillerOrder)
        {
            var result = new Dictionary<ServiceRequestStatKey, int>(new KeyComparer());

            // Build standard FHIR search path:
            // ServiceRequest?authored=ge{start}&authored=le{end}&_profile={profile}&status:not=revoked&status:not=entered-in-error&_count=200[&intent=filler-order]
            var startIso = Uri.EscapeDataString(filter.Start.ToString("o"));
            var endIso = Uri.EscapeDataString(filter.End.ToString("o"));
            var profile = Uri.EscapeDataString(filter.ProfileUrl);

            var query = $"ServiceRequest?authored=ge{startIso}&authored=le{endIso}&_profile={profile}&status:not=revoked&status:not=entered-in-error&_count=200";
            if (includeIntentFillerOrder)
            {
                query += "&intent=filler-order";
            }

            string? nextUrl = query;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                using var resp = await _http.GetAsync(nextUrl);
                if (!resp.IsSuccessStatusCode) break;

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var bundle = await JsonSerializer.DeserializeAsync<FhirBundle>(stream, JsonOpts);
                if (bundle?.Entry != null)
                {
                    foreach (var e in bundle.Entry)
                    {
                        var sr = e.Resource;
                        if (sr == null) continue;

                        // Defensive filters
                        if (sr.Meta?.Profile == null || !sr.Meta.Profile.Contains(filter.ProfileUrl))
                            continue;

                        var status = (sr.Status ?? "").Trim().ToLowerInvariant();
                        if (status == "revoked" || status == "entered-in-error")
                            continue;

                        if (includeIntentFillerOrder)
                        {
                            var intent = (sr.Intent ?? "").Trim().ToLowerInvariant();
                            if (intent != "filler-order") continue;
                        }

                        // authoredOn within range (defensive re-check)
                        if (!string.IsNullOrWhiteSpace(sr.AuthoredOn))
                        {
                            if (DateTime.TryParse(sr.AuthoredOn, out var authored))
                            {
                                if (authored < filter.Start || authored > filter.End) continue;
                            }
                        }

                        var deptId = ExtractOrganizationId(sr.Requester);
                        if (filter.ExecOrgIds != null && filter.ExecOrgIds.Count > 0)
                        {
                            if (string.IsNullOrEmpty(deptId) || !filter.ExecOrgIds.Contains(deptId))
                                continue;
                        }

                        var doctorId = ExtractFirstPractitionerId(sr.SupportingInfo);
                        if (filter.DoctorIds != null && filter.DoctorIds.Count > 0)
                        {
                            if (string.IsNullOrEmpty(doctorId) || !filter.DoctorIds.Contains(doctorId))
                                continue;
                        }

                        var key = new ServiceRequestStatKey
                        {
                            DepartmentId = deptId,
                            DoctorId = doctorId
                        };

                        if (!result.ContainsKey(key))
                            result[key] = 0;
                        result[key] += 1;
                    }
                }

                nextUrl = bundle?.GetNextLink();
            }

            return result;
        }

        private static string ExtractOrganizationId(FhirReference? requester)
        {
            if (requester?.Reference == null) return "";
            var parts = requester.Reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals("Organization", StringComparison.OrdinalIgnoreCase))
                return parts[1];
            return "";
        }

        private static string ExtractFirstPractitionerId(List<FhirReference>? supportingInfo)
        {
            if (supportingInfo == null || supportingInfo.Count == 0) return "";
            var first = supportingInfo[0];
            if (first?.Reference == null) return "";
            var parts = first.Reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals("Practitioner", StringComparison.OrdinalIgnoreCase))
                return parts[1];
            return "";
        }

        // FHIR minimal models
        private class FhirBundle
        {
            [JsonPropertyName("resourceType")]
            public string? ResourceType { get; set; }

            [JsonPropertyName("link")]
            public List<FhirBundleLink>? Link { get; set; }

            [JsonPropertyName("entry")]
            public List<FhirBundleEntry>? Entry { get; set; }

            public string? GetNextLink()
            {
                if (Link == null) return null;
                foreach (var l in Link)
                {
                    if (string.Equals(l.Relation, "next", StringComparison.OrdinalIgnoreCase))
                        return l.Url;
                }
                return null;
            }
        }

        private class FhirBundleLink
        {
            [JsonPropertyName("relation")]
            public string? Relation { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }
        }

        private class FhirBundleEntry
        {
            [JsonPropertyName("resource")]
            public FhirServiceRequest? Resource { get; set; }
        }

        private class FhirServiceRequest
        {
            [JsonPropertyName("meta")]
            public FhirMeta? Meta { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("intent")]
            public string? Intent { get; set; }

            [JsonPropertyName("authoredOn")]
            public string? AuthoredOn { get; set; }

            [JsonPropertyName("requester")]
            public FhirReference? Requester { get; set; }

            [JsonPropertyName("supportingInfo")]
            public List<FhirReference>? SupportingInfo { get; set; }
        }

        private class FhirMeta
        {
            [JsonPropertyName("profile")]
            public List<string>? Profile { get; set; }
        }

        private class FhirReference
        {
            [JsonPropertyName("reference")]
            public string? Reference { get; set; }
        }

        private class KeyComparer : IEqualityComparer<ServiceRequestStatKey>
        {
            public bool Equals(ServiceRequestStatKey? x, ServiceRequestStatKey? y)
            {
                if (x == null || y == null) return false;
                return string.Equals(x.DepartmentId, y.DepartmentId, StringComparison.Ordinal) &&
                       string.Equals(x.DoctorId, y.DoctorId, StringComparison.Ordinal);
            }

            public int GetHashCode(ServiceRequestStatKey obj)
            {
                unchecked
                {
                    return ((obj.DepartmentId?.GetHashCode() ?? 0) * 397) ^ (obj.DoctorId?.GetHashCode() ?? 0);
                }
            }
        }
    }
}