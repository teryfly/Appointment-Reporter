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
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<OutpatientReportService>();
            services.AddScoped<MedicalTechReportService>();
            services.AddScoped<DoctorAnalysisReportService>();
            // External API
            services.AddScoped<IOrganizationApiClient, OrganizationApiClient>();
            // 👇👇👇 必须注册HttpClientFactory（用于注入IHttpClientFactory及命名客户端）
            services.AddHttpClient(nameof(OrganizationApiClient));
            // Controllers
            services.AddControllers();
            // Memory cache
            services.AddMemoryCache();
        }
    }
}