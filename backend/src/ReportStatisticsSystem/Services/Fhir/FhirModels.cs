using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Services.Fhir
{
    // Minimal FHIR Bundle representations
    public class FhirBundle
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

    public class FhirBundleLink
    {
        [JsonPropertyName("relation")]
        public string? Relation { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class FhirBundleEntry
    {
        [JsonPropertyName("resource")]
        public FhirPractitioner? Resource { get; set; }
    }

    // Minimal FHIR Practitioner
    public class FhirPractitioner
    {
        [JsonPropertyName("resourceType")]
        public string? ResourceType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public List<HumanName>? Name { get; set; }
    }

    public class HumanName
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("family")]
        public string? Family { get; set; }

        [JsonPropertyName("given")]
        public List<string>? Given { get; set; }
    }

    public static class FhirMapping
    {
        // Prefer name.text; else family+given; else empty string
        public static string MapHumanName(FhirPractitioner p)
        {
            if (p.Name != null && p.Name.Count > 0)
            {
                var hn = p.Name[0];
                if (!string.IsNullOrWhiteSpace(hn.Text))
                    return hn.Text.Trim();

                var family = hn.Family ?? "";
                var given = hn.Given != null ? string.Join("", hn.Given) : "";
                var merged = $"{family}{given}".Trim();
                if (!string.IsNullOrWhiteSpace(merged))
                    return merged;
            }
            return "";
        }
    }
}