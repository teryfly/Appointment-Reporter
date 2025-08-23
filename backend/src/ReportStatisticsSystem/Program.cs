using Configuration;
using Infrastructure.DependencyInjection;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// 配置绑定
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<ExternalApiConfig>(builder.Configuration.GetSection("ExternalApi"));
builder.Services.Configure<CacheConfig>(builder.Configuration.GetSection("Cache"));

// 服务注册
ServiceRegistration.Register(builder.Services, builder.Configuration);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "报表统计系统 API",
        Version = "v1",
        Description = "基于ASP.NET Core 8.0的六大医疗预约统计报表API"
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, true);
});

// 明确设置 https 端口以便需要时重定向可用
var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"] ?? "7171";
builder.WebHost.UseSetting("https_port", httpsPort);

var app = builder.Build();

// 中间件
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "报表统计系统 API v1");
        c.RoutePrefix = string.Empty;
    });
    // 开发环境默认不强制 HTTPS，避免“Failed to determine the https port for redirect.”
    // 如需强制 https，请取消下一行注释
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.Run();