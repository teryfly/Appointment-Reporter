using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Services;
using Services.Reports;
using Repositories.Interfaces;
using Repositories;
using ExternalServices.Interfaces;
using ExternalServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using Services.Fhir;

namespace Infrastructure.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<ReportDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();

            // Services
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<OutpatientReportService>();
            services.AddScoped<MedicalTechReportService>();
            services.AddScoped<DoctorAnalysisReportService>();
            services.AddScoped<IOrganizationService, OrganizationService>();

            // External API
            services.AddScoped<IOrganizationApiClient, OrganizationApiClient>();

            // HttpClientFactory registrations
            services.AddHttpClient(nameof(OrganizationApiClient));

            // FHIR HttpClient
            services.AddHttpClient("FhirClient", client =>
            {
                var baseUrl = configuration.GetSection("Fhir")["BaseUrl"] ?? "http://localhost:8080/fhir";
                client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                var timeoutSeconds = int.TryParse(configuration.GetSection("Fhir")["TimeoutSeconds"], out var t) ? t : 10;
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            });

            // Practitioner service (existing)
            services.AddScoped<IPractitionerService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirPractitionerService(http);
            });

            // FHIR Lookup service
            services.AddScoped<IFhirLookupService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirLookupService(http);
            });

            // FHIR ServiceRequest service
            services.AddScoped<IFhirServiceRequestService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirServiceRequestService(http);
            });

            // FHIR Medical-Tech aggregation service (existing)
            services.AddScoped<FhirMedicalTechAggregationService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirMedicalTechAggregationService(http);
            });

            // New: FHIR ServiceRequest source-type aggregation service
            services.AddScoped<FhirServiceRequestSourceAggregationService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirServiceRequestSourceAggregationService(http);
            });

            // New: FHIR Medical-Tech item aggregation service
            services.AddScoped<FhirMedicalTechItemAggregationService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpClientFactory.CreateClient("FhirClient");
                return new FhirMedicalTechItemAggregationService(http);
            });

            // Controllers
            services.AddControllers();

            // Memory cache
            services.AddMemoryCache();

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("FrontendCors", builder =>
                {
                    builder
                        .WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173",
                            "http://localhost:5174",
                            "http://127.0.0.1:5174",
                            "http://localhost:5175",
                            "http://127.0.0.1:5175"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }
    }

    // Robust FHIR Practitioner service implementation
    internal class FhirPractitionerService : IPractitionerService
    {
        private readonly HttpClient _http;
        private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FhirPractitionerService(HttpClient http)
        {
            _http = http;
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.List<PractitionerDto>> GetPractitionersAsync(System.Collections.Generic.List<string>? ids)
        {
            if (ids != null && ids.Count > 0)
            {
                var set = new System.Collections.Generic.HashSet<string>(ids);
                var results = new System.Collections.Generic.List<PractitionerDto>();
                foreach (var id in set)
                {
                    var p = await GetOneAsync(id);
                    if (p != null) results.Add(p);
                }
                return results;
            }

            var list = new System.Collections.Generic.List<PractitionerDto>();
            string? nextUrl = "Practitioner?_count=200";
            while (!string.IsNullOrEmpty(nextUrl))
            {
                using var resp = await _http.GetAsync(nextUrl);
                if (!resp.IsSuccessStatusCode) break;

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var bundle = await System.Text.Json.JsonSerializer.DeserializeAsync<Services.Fhir.FhirBundle>(stream, JsonOpts);
                if (bundle?.Entry != null)
                {
                    foreach (var e in bundle.Entry)
                    {
                        var dto = Map(e.Resource);
                        if (dto != null) list.Add(dto);
                    }
                }
                nextUrl = bundle?.GetNextLink();
            }
            return list;
        }

        private async System.Threading.Tasks.Task<PractitionerDto?> GetOneAsync(string id)
        {
            using var resp = await _http.GetAsync($"Practitioner/{Uri.EscapeDataString(id)}");
            if (!resp.IsSuccessStatusCode) return null;

            await using var stream = await resp.Content.ReadAsStreamAsync();
            var p = await System.Text.Json.JsonSerializer.DeserializeAsync<Services.Fhir.FhirPractitioner>(stream, JsonOpts);
            return Map(p);
        }

        private static PractitionerDto? Map(Services.Fhir.FhirPractitioner? p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Id)) return null;
            var name = Services.Fhir.FhirMapping.MapHumanName(p);
            return new PractitionerDto { Id = p.Id, Name = name };
        }
    }
}