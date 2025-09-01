using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Services.Fhir
{
    // Aggregates ServiceRequest counts by (slot, performer org, item code/display) and source-type (1/2/3)
    // slot depends on groupBy: day => yyyy-MM-dd, month => yyyy-MM, year => yyyy
    public class FhirMedicalTechItemAggregationService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private const string ProfileUrl = "http://StructureDefinition.hl7fhir.cn/inspect-request";
        private const string SourceTypeSystem = "http://CodeSystem.hl7fhir.cn/application-form-source-type";

        public FhirMedicalTechItemAggregationService(HttpClient http)
        {
            _http = http;
            if (!JsonOpts.Converters.Any(c => c is ReferenceOrArrayConverter))
                JsonOpts.Converters.Add(new ReferenceOrArrayConverter());
        }

        public class Row
        {
            public string Slot { get; set; } = string.Empty;
            public string OrgId { get; set; } = string.Empty;
            public string ItemCode { get; set; } = string.Empty;
            public string ItemDisplay { get; set; } = string.Empty;
            public int Outpatient { get; set; }   // source-type code = 1
            public int Inpatient { get; set; }    // source-type code = 2
            public int PhysicalExam { get; set; } // source-type code = 3
        }

        public async Task<List<Row>> AggregateAsync(
            DateTime start,
            DateTime end,
            string groupBy,
            List<string>? performerOrgIds,
            List<string>? itemCodes)
        {
            // key: (Slot, OrgId, ItemCode, ItemDisplay)
            var dict = new Dictionary<(string Slot, string OrgId, string ItemCode, string ItemDisplay), (int Out, int Inp, int Phy)>();

            var startIso = Uri.EscapeDataString(start.ToString("o"));
            var endIso = Uri.EscapeDataString(end.ToString("o"));
            var profile = Uri.EscapeDataString(ProfileUrl);

            // Base query; use occurrence[x], profile, status guards and intent filter; page by _count
            var query = $"ServiceRequest?occurrence=ge{startIso}&occurrence=le{endIso}&_profile={profile}&status:not=revoked&status:not=entered-in-error&intent=filler-order&_count=200";

            string? nextUrl = query;

            // HashSets for quick filtering
            HashSet<string>? performerFilter = performerOrgIds != null && performerOrgIds.Count > 0
                ? new HashSet<string>(performerOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                : null;

            HashSet<string>? itemCodeFilter = itemCodes != null && itemCodes.Count > 0
                ? new HashSet<string>(itemCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
                : null;

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

                        // occurrence within range
                        var occ = ExtractOccurrence(sr);
                        if (occ == null) continue;
                        var occurred = occ.Value;
                        if (occurred < start || occurred > end) continue;

                        var slot = groupBy switch
                        {
                            "year" => occurred.Year.ToString(),
                            "month" => $"{occurred.Year}-{occurred.Month:D2}",
                            _ => occurred.ToString("yyyy-MM-dd")
                        };

                        // performer orgs
                        var performers = ExtractPerformerOrganizationIds(sr.Performer);
                        if (performerFilter != null)
                        {
                            performers = performers.Where(id => performerFilter.Contains(id)).ToList();
                            if (performers.Count == 0) continue;
                        }
                        if (performers.Count == 0) continue;

                        // item code/display from ServiceRequest.code.coding[*]
                        var (codes, displays) = ExtractItemCodeAndDisplay(sr.Code);
                        if (codes.Count == 0 && displays.Count == 0)
                        {
                            // if none, use empty placeholders to still allow counting (rare)
                            codes.Add("");
                            displays.Add("");
                        }

                        // If itemCode filter exists, keep only codings matching filter
                        if (itemCodeFilter != null)
                        {
                            var zipped = codes.Zip(displays, (c, d) => (c, d))
                                              .Where(z => !string.IsNullOrWhiteSpace(z.c) && itemCodeFilter.Contains(z.c))
                                              .ToList();
                            if (zipped.Count == 0) continue;
                            codes = zipped.Select(z => z.c).ToList();
                            displays = zipped.Select(z => z.d).ToList();
                        }

                        // source-type flags from category system
                        var srcCodes = ExtractSourceTypeCodes(sr.Category);
                        var hasOut = srcCodes.Contains("1");
                        var hasInp = srcCodes.Contains("2");
                        var hasPhy = srcCodes.Contains("3");

                        if (!hasOut && !hasInp && !hasPhy) continue;

                        // For each performer and each item coding, increment counters
                        for (int i = 0; i < codes.Count; i++)
                        {
                            var code = codes[i] ?? "";
                            var disp = (displays.ElementAtOrDefault(i) ?? "").Trim();
                            foreach (var orgId in performers)
                            {
                                var key = (Slot: slot, OrgId: orgId, ItemCode: code, ItemDisplay: disp);
                                var tuple = dict.TryGetValue(key, out var v) ? v : (0, 0, 0);
                                if (hasOut) tuple.Item1++;
                                if (hasInp) tuple.Item2++;
                                if (hasPhy) tuple.Item3++;
                                dict[key] = tuple;
                            }
                        }
                    }
                }

                nextUrl = bundle?.GetNextLink();
            }

            var rows = dict.Select(kv => new Row
            {
                Slot = kv.Key.Slot,
                OrgId = kv.Key.OrgId,
                ItemCode = kv.Key.ItemCode,
                ItemDisplay = kv.Key.ItemDisplay,
                Outpatient = kv.Value.Item1,
                Inpatient = kv.Value.Item2,
                PhysicalExam = kv.Value.Item3
            })
            .OrderBy(r => r.Slot)
            .ThenBy(r => r.OrgId)
            .ThenBy(r => r.ItemCode)
            .ThenBy(r => r.ItemDisplay)
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

        private static (List<string> Codes, List<string> Displays) ExtractItemCodeAndDisplay(FhirCodeableConcept? codeConcept)
        {
            var codes = new List<string>();
            var displays = new List<string>();
            if (codeConcept?.Coding != null)
            {
                foreach (var c in codeConcept.Coding)
                {
                    if (c == null) continue;
                    codes.Add(c.Code ?? "");
                    var disp = (c.Display ?? "").Trim();
                    displays.Add(disp);
                }
            }
            return (codes, displays);
        }

        private static HashSet<string> ExtractSourceTypeCodes(List<FhirCodeableConcept>? categories)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (categories == null) return set;
            foreach (var cc in categories)
            {
                if (cc?.Coding == null) continue;
                foreach (var cod in cc.Coding)
                {
                    if (cod == null) continue;
                    if (string.Equals(cod.System, SourceTypeSystem, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(cod.Code))
                            set.Add(cod.Code!);
                    }
                }
            }
            return set;
        }

        // Minimal FHIR models for ServiceRequest we need
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

            [JsonPropertyName("code")]
            public FhirCodeableConcept? Code { get; set; }
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