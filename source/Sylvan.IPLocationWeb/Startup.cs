using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sylvan.IPLocation;

namespace IPLocationWeb;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(
            o =>
            {
                o.OutputFormatters.Insert(0, new JsonDataOutputFormatter());
                o.AddSylvanCsvFormatters();
                o.AddSylvanExcelFormatters();
            }
        );

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IPLocationWeb", Version = "v1" });
        });
        var db = new Database(@"C:\data\IPDb\IP2LOCATION-LITE-DB11.IPV6.BIN\IP2LOCATION-LITE-DB11.IPV6.BIN");
        services.AddSingleton(db);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage(); 
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IPLocationWeb v1"));

        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
