﻿using System.Linq;
using FilterLists.Api.DependencyInjection.Extensions;
using FilterLists.Data;
using FilterLists.Data.Seed.Extensions;
using FilterLists.Services.DependencyInjection.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace FilterLists.Api
{
    [UsedImplicitly]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddFilterListsServices(Configuration);
            services.AddFilterListsApi();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseMvc();
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Host = httpReq.Host.Value);
                //TODO: remove preprocessor directives
#if RELEASE
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.BasePath = "/api");
#endif
                c.RouteTemplate = "docs/{documentName}/swagger.json";
                UseLowercaseControllerNameInSwaggerHack(c);
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "FilterLists API v1");
                c.DocumentTitle = "FilterLists API v1";
                c.RoutePrefix = "docs";
            });
            MigrateAndSeedDatabase(app);
        }

        //TODO: remove hack (https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/74#issuecomment-386762178)
        private static void UseLowercaseControllerNameInSwaggerHack(SwaggerOptions c)
        {
            c.PreSerializeFilters.Add((document, request) =>
            {
                var paths = document.Paths.ToDictionary(item => item.Key.ToLowerInvariant(), item => item.Value);
                document.Paths.Clear();
                foreach (var pathItem in paths) document.Paths.Add(pathItem.Key, pathItem.Value);
            });
        }

        private void MigrateAndSeedDatabase(IApplicationBuilder app)
        {
            var dataPath = Configuration.GetSection("DataDirectory").GetValue<string>("Path");
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetService<FilterListsDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetService<FilterListsDbContext>().SeedOrUpdate(dataPath);
            }
        }
    }
}