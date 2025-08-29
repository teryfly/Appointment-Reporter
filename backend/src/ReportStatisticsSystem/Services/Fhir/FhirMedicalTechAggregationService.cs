using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Services.Fhir
{
    public class FhirMedicalTechAggregationService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FhirMedicalTechAggregationService(HttpClient http)
        {
            _http = http;
            if (!JsonOpts.Converters.Any(c => c is ReferenceOrArrayConverter))
                JsonOpts.Converters.Add(new ReferenceOrArrayConverter());
        }

        public class AppointmentRow
        {
            public string Slot { get; set; } = string.Empty;
            public string DepartmentId { get; set; } = string.Empty;
            public string CategoryDisplay { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public async Task<List<AppointmentRow>> QueryAppointmentsAsync(
            DateTime occurrenceStart,
            DateTime occurrenceEnd,
            string groupBy,
            List<string>? performerOrgIds,
            string profileUrl = "http://StructureDefinition.hl7fhir.cn/inspect-request",
            string categorySystem = "http://CodeSystem.hl7fhir.cn/inspect-category")
        {
            var dict = new Dictionary<(string Slot, string DeptId, string CatDisp), int>();

            var startIso = Uri.EscapeDataString(occurrenceStart.ToString("o"));
            var endIso = Uri.EscapeDataString(occurrenceEnd.ToString("o"));
            var profile = Uri.EscapeDataString(profileUrl);

            var baseQuery = $"ServiceRequest?date=ge{startIso}&date=le{endIso}&_profile={profile}&status:not=revoked&intent=filler-order&_count=200";

            string? nextUrl = baseQuery;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                using var resp = await _http.GetAsync(nextUrl);
                if (!resp.IsSuccessStatusCode) break;

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var bundle = await JsonSerializer.DeserializeAsync<FhirBundle>(stream, JsonOpts);

                if (bundle?.Entry != null)
                {
                    foreach (var entry in bundle.Entry)
                    {
                        var sr = entry.Resource;
                        if (sr == null) continue;

                        if (sr.Meta?.Profile == null || !sr.Meta.Profile.Contains(profileUrl))
                            continue;

                        var status = (sr.Status ?? "").Trim().ToLowerInvariant();
                        if (status == "revoked") continue;

                        var intent = (sr.Intent ?? "").Trim().ToLowerInvariant();
                        if (intent != "filler-order") continue;

                        var occ = ExtractOccurrence(sr);
                        if (occ == null) continue;
                        var occurredAt = occ.Value;
                        if (occurredAt < occurrenceStart || occurredAt > occurrenceEnd) continue;

                        var slot = groupBy switch
                        {
                            "year" => occurredAt.Year.ToString(),
                            "month" => $"{occurredAt.Year}-{occurredAt.Month:D2}",
                            _ => occurredAt.ToString("yyyy-MM-dd")
                        };

                        var deptId = ExtractPerformerOrganizationId(sr.Performer);
                        if (performerOrgIds != null && performerOrgIds.Count > 0)
                        {
                            if (string.IsNullOrEmpty(deptId) || !performerOrgIds.Contains(deptId))
                                continue;
                        }

                        var categoryDisplay = ExtractCategoryDisplay(sr.Category, categorySystem) ?? "";

                        var key = (slot, deptId, categoryDisplay);
                        if (!dict.ContainsKey(key)) dict[key] = 0;
                        dict[key] += 1;
                    }
                }

                nextUrl = bundle?.GetNextLink();
            }

            var rows = dict
                .Select(kv => new AppointmentRow
                {
                    Slot = kv.Key.Slot,
                    DepartmentId = kv.Key.DeptId,
                    CategoryDisplay = kv.Key.CatDisp,
                    Count = kv.Value
                })
                .OrderBy(r => r.Slot)
                .ThenBy(r => r.DepartmentId)
                .ThenBy(r => r.CategoryDisplay)
                .ToList();

            return rows;
        }

        private static DateTime? ExtractOccurrence(FhirServiceRequest sr)
        {
            if (!string.IsNullOrWhiteSpace(sr.OccurrenceDateTime))
            {
                if (DateTime.TryParse(sr.OccurrenceDateTime, out var dt))
                    return dt;
            }
            if (sr.OccurrencePeriod != null)
            {
                if (!string.IsNullOrWhiteSpace(sr.OccurrencePeriod.Start) && DateTime.TryParse(sr.OccurrencePeriod.Start, out var s))
                    return s;
                if (!string.IsNullOrWhiteSpace(sr.OccurrencePeriod.End) && DateTime.TryParse(sr.OccurrencePeriod.End, out var e))
                    return e;
            }
            return null;
        }

        private static string ExtractPerformerOrganizationId(List<FhirReference>? performerList)
        {
            if (performerList == null || performerList.Count == 0) return "";
            foreach (var r in performerList)
            {
                var id = ExtractOrganizationId(r);
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return "";
        }

        private static string ExtractOrganizationId(FhirReference? reference)
        {
            if (reference?.Reference == null) return "";
            var parts = reference.Reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals("Organization", StringComparison.OrdinalIgnoreCase))
                return parts[1];
            return "";
        }

        private static string? ExtractCategoryDisplay(List<FhirCodeableConcept>? categories, string categorySystem)
        {
            if (categories == null) return null;
            foreach (var cc in categories)
            {
                if (cc?.Coding == null) continue;
                foreach (var coding in cc.Coding)
                {
                    if (coding == null) continue;
                    if (string.Equals(coding.System, categorySystem, StringComparison.OrdinalIgnoreCase))
                    {
                        var display = (coding.Display ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(display)) return display;
                        if (!string.IsNullOrWhiteSpace(coding.Code)) return coding.Code!;
                    }
                }
            }
            return null;
        }

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
            public FhirServiceRequestMeta? Meta { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("intent")]
            public string? Intent { get; set; }

            [JsonPropertyName("occurrenceDateTime")]
            public string? OccurrenceDateTime { get; set; }

            [JsonPropertyName("occurrencePeriod")]
            public FhirPeriod? OccurrencePeriod { get; set; }

            [JsonPropertyName("performer")]
            [JsonConverter(typeof(ReferenceOrArrayConverter))]
            public List<FhirReference>? Performer { get; set; }

            [JsonPropertyName("category")]
            public List<FhirCodeableConcept>? Category { get; set; }
        }

        private class FhirServiceRequestMeta
        {
            [JsonPropertyName("profile")]
            public List<string>? Profile { get; set; }
        }

        private class FhirPeriod
        {
            [JsonPropertyName("start")]
            public string? Start { get; set; }

            [JsonPropertyName("end")]
            public string? End { get; set; }
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

        private class ReferenceOrArrayConverter : JsonConverter<List<FhirReference>?>
        {
            public override List<FhirReference>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var list = new List<FhirReference>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray) break;
                        var item = JsonSerializer.Deserialize<FhirReference>(ref reader, options);
                        if (item != null) list.Add(item);
                    }
                    return list;
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var single = JsonSerializer.Deserialize<FhirReference>(ref reader, options);
                    return single != null ? new List<FhirReference> { single } : new List<FhirReference>();
                }

                reader.Skip();
                return new List<FhirReference>();
            }

            public override void Write(Utf8JsonWriter writer, List<FhirReference>? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                writer.WriteStartArray();
                foreach (var r in value)
                {
                    JsonSerializer.Serialize(writer, r, options);
                }
                writer.WriteEndArray();
            }
        }
    }
}