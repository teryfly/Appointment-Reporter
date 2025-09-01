using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Services.Fhir
{
    // Aggregates ServiceRequest counts by (performer org, time slot) for source types (1/2/3)
    public class FhirServiceRequestSourceAggregationService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private const string ProfileUrl = "http://StructureDefinition.hl7fhir.cn/inspect-request";
        private const string SourceTypeSystem = "http://CodeSystem.hl7fhir.cn/application-form-source-type";

        public FhirServiceRequestSourceAggregationService(HttpClient http)
        {
            _http = http;
            if (!JsonOpts.Converters.Any(c => c is ReferenceOrArrayConverter))
                JsonOpts.Converters.Add(new ReferenceOrArrayConverter());
        }

        public class Row
        {
            public string OrgId { get; set; } = string.Empty;
            public string Slot { get; set; } = string.Empty;
            public int Outpatient { get; set; }    // code=1
            public int Inpatient { get; set; }     // code=2
            public int PhysicalExam { get; set; }  // code=3
        }

        public async Task<List<Row>> AggregateRowsAsync(
            DateTime start,
            DateTime end,
            string groupBy,
            List<string>? performerOrgIds)
        {
            // Use tuple key (OrgId, Slot); default comparer for ValueTuple handles string keys appropriately.
            var dict = new Dictionary<(string OrgId, string Slot), (int, int, int)>();

            var startIso = Uri.EscapeDataString(start.ToString("o"));
            var endIso = Uri.EscapeDataString(end.ToString("o"));
            var profile = Uri.EscapeDataString(ProfileUrl);

            // FHIR standard search: use occurrence[x] range, status filter, intent=filler-order
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
                    foreach (var entry in bundle.Entry)
                    {
                        var sr = entry.Resource;
                        if (sr == null) continue;

                        // profile/status/intent guards
                        if (sr.Meta?.Profile == null || !sr.Meta.Profile.Contains(ProfileUrl))
                            continue;

                        var status = (sr.Status ?? "").Trim().ToLowerInvariant();
                        if (status == "revoked" || status == "entered-in-error") continue;

                        var intent = (sr.Intent ?? "").Trim().ToLowerInvariant();
                        if (intent != "filler-order") continue;

                        // occurrence moment
                        var occuredAt = ExtractOccurrence(sr);
                        if (occuredAt == null) continue;
                        var occured = occuredAt.Value;
                        if (occured < start || occured > end) continue;

                        var slot = groupBy switch
                        {
                            "year" => occured.Year.ToString(),
                            "month" => $"{occured.Year}-{occured.Month:D2}",
                            _ => occured.ToString("yyyy-MM-dd")
                        };

                        // performer org ids
                        var performers = ExtractPerformerOrganizationIds(sr.Performer);
                        if (performerOrgIds != null && performerOrgIds.Count > 0)
                        {
                            var set = new HashSet<string>(performerOrgIds);
                            performers = performers.Where(id => set.Contains(id)).ToList();
                            if (performers.Count == 0) continue;
                        }
                        if (performers.Count == 0) continue;

                        // source-type codes
                        var sourceCodes = ExtractSourceTypeCodes(sr.Category);
                        if (sourceCodes.Count == 0) continue;

                        var hasOut = sourceCodes.Contains("1");
                        var hasInp = sourceCodes.Contains("2");
                        var hasPhy = sourceCodes.Contains("3");

                        foreach (var orgId in performers)
                        {
                            var key = (OrgId: orgId, Slot: slot);
                            var val = dict.TryGetValue(key, out var v) ? v : (0, 0, 0);
                            if (hasOut) val.Item1++;
                            if (hasInp) val.Item2++;
                            if (hasPhy) val.Item3++;
                            dict[key] = val;
                        }
                    }
                }

                nextUrl = bundle?.GetNextLink();
            }

            var rows = dict.Select(kv => new Row
            {
                OrgId = kv.Key.OrgId,
                Slot = kv.Key.Slot,
                Outpatient = kv.Value.Item1,
                Inpatient = kv.Value.Item2,
                PhysicalExam = kv.Value.Item3
            })
            .OrderBy(r => r.OrgId)
            .ThenBy(r => r.Slot)
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

        private static List<string> ExtractPerformerOrganizationIds(List<FhirReference>? performerList)
        {
            var list = new List<string>();
            if (performerList == null) return list;
            foreach (var r in performerList)
            {
                var id = ExtractOrganizationId(r);
                if (!string.IsNullOrEmpty(id)) list.Add(id);
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

        private static HashSet<string> ExtractSourceTypeCodes(List<FhirCodeableConcept>? categories)
        {
            var codes = new HashSet<string>(StringComparer.Ordinal);
            if (categories == null) return codes;
            foreach (var cc in categories)
            {
                if (cc?.Coding == null) continue;
                foreach (var cod in cc.Coding)
                {
                    if (cod == null) continue;
                    if (string.Equals(cod.System, SourceTypeSystem, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(cod.Code))
                            codes.Add(cod.Code!);
                    }
                }
            }
            return codes;
        }

        // Minimal FHIR models tailored for this aggregation
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

        private class FhirMeta
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
                if (reader.TokenType == JsonTokenType.StartObject)
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