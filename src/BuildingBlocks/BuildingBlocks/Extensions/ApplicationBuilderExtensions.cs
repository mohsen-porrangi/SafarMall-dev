using BuildingBlocks.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CurrentUserMiddleware>();
        }

        public static IApplicationBuilder UseSwaggerExtention(this IApplicationBuilder app, string title = "API", string version = "v1")
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    var basePath = httpReq.PathBase.HasValue ? httpReq.PathBase.Value : string.Empty;
                    swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{basePath}" }
                };
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");
                c.RoutePrefix = "swagger";
            });

            return app;
        }
        //public static IApplicationBuilder UserSwaggerApiGateway(this IApplicationBuilder app)
        //{

        //}
    }
}

