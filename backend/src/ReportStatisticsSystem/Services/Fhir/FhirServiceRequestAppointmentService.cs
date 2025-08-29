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
    internal class FhirServiceRequestAppointmentService : IFhirServiceRequestAppointmentService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private const string CategorySystem = "http://CodeSystem.hl7fhir.cn/inspect-category";

        public FhirServiceRequestAppointmentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<Dictionary<AppointmentStatKey, int>> CountAppointmentsAsync(ServiceRequestAppointmentFilter filter)
        {
            var result = new Dictionary<AppointmentStatKey, int>(new KeyComparer());

            // Standard FHIR search:
            // ServiceRequest?occurrence=ge{start}&occurrence=le{end}&_profile={profile}&status:not=revoked&status:not=entered-in-error&intent=filler-order&_count=200
            var startIso = Uri.EscapeDataString(filter.Start.ToString("o"));
            var endIso = Uri.EscapeDataString(filter.End.ToString("o"));
            var profile = Uri.EscapeDataString(filter.ProfileUrl);
            var query = $"ServiceRequest?occurrence=ge{startIso}&occurrence=le{endIso}&_profile={profile}&status:not=revoked&status:not=entered-in-error&intent=filler-order&_count=200";

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

                        var intent = (sr.Intent ?? "").Trim().ToLowerInvariant();
                        if (intent != "filler-order") continue;

                        // Occurrence in range (defensive)
                        if (!string.IsNullOrWhiteSpace(sr.OccurrenceDateTime))
                        {
                            if (DateTime.TryParse(sr.OccurrenceDateTime, out var occ))
                            {
                                if (occ < filter.Start || occ > filter.End) continue;
                            }
                        }

                        // Performer org id(s): performer is Reference or array; we accept Organization only, and if multiple we count each line towards each performer? 
                        // Requirement says "按预约时间及执行科室检查类型统计预约量", treat each SR per performer org. If multiple, count for each performer org.
                        var performerOrgIds = ExtractPerformerOrgIds(sr.Performer);
                        if (filter.ExecOrgIds != null && filter.ExecOrgIds.Count > 0)
                        {
                            performerOrgIds = performerOrgIds.Where(id => filter.ExecOrgIds.Contains(id)).ToList();
                            if (performerOrgIds.Count == 0) continue;
                        }
                        if (performerOrgIds.Count == 0)
                        {
                            // no performer org; skip
                            continue;
                        }

                        // Category code(s) from specified system
                        var categoryCodes = ExtractCategoryCodes(sr.Category, CategorySystem);
                        if (categoryCodes.Count == 0)
                        {
                            // if no category for given system, count under empty code
                            categoryCodes.Add("");
                        }

                        // For each performer org and each category code, increment
                        foreach (var orgId in performerOrgIds)
                        {
                            foreach (var code in categoryCodes)
                            {
                                var key = new AppointmentStatKey { PerformerOrgId = orgId, CategoryCode = code ?? "" };
                                if (!result.ContainsKey(key))
                                    result[key] = 0;
                                result[key] += 1;
                            }
                        }
                    }
                }

                nextUrl = bundle?.GetNextLink();
            }

            return result;
        }

        private static List<string> ExtractPerformerOrgIds(List<FhirReference>? performer)
        {
            var list = new List<string>();
            if (performer == null) return list;
            foreach (var r in performer)
            {
                var id = ExtractOrganizationId(r);
                if (!string.IsNullOrEmpty(id))
                    list.Add(id);
            }
            return list.Distinct().ToList();
        }

        private static string ExtractOrganizationId(FhirReference? reference)
        {
            if (reference?.Reference == null) return "";
            var parts = reference.Reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals("Organization", StringComparison.OrdinalIgnoreCase))
                return parts[1];
            return "";
        }

        private static List<string> ExtractCategoryCodes(List<FhirCodeableConcept>? categories, string system)
        {
            var codes = new List<string>();
            if (categories == null) return codes;
            foreach (var cat in categories)
            {
                if (cat.Coding == null) continue;
                foreach (var c in cat.Coding)
                {
                    if (string.Equals(c.System, system, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(c.Code))
                            codes.Add(c.Code);
                    }
                }
            }
            return codes.Distinct().ToList();
        }

        // Minimal models
        private class FhirBundle
        {
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
            [JsonPropertyName("occurrenceDateTime")]
            public string? OccurrenceDateTime { get; set; }
            [JsonPropertyName("performer")]
            public List<FhirReference>? Performer { get; set; }
            [JsonPropertyName("category")]
            public List<FhirCodeableConcept>? Category { get; set; }
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

        private class FhirCodeableConcept
        {
            [JsonPropertyName("coding")]
            public List<FhirCoding>? Coding { get; set; }
        }

        private class FhirCoding
        {
            [JsonPropertyName("system")]
            public string? System { get; set; }
            [JsonPropertyName("code")]
            public string? Code { get; set; }
            [JsonPropertyName("display")]
            public string? Display { get; set; }
        }

        private class KeyComparer : IEqualityComparer<AppointmentStatKey>
        {
            public bool Equals(AppointmentStatKey? x, AppointmentStatKey? y)
            {
                if (x == null || y == null) return false;
                return string.Equals(x.PerformerOrgId, y.PerformerOrgId, StringComparison.Ordinal) &&
                       string.Equals(x.CategoryCode, y.CategoryCode, StringComparison.Ordinal);
            }

            public int GetHashCode(AppointmentStatKey obj)
            {
                unchecked
                {
                    return ((obj.PerformerOrgId?.GetHashCode() ?? 0) * 397) ^ (obj.CategoryCode?.GetHashCode() ?? 0);
                }
            }
        }
    }
}