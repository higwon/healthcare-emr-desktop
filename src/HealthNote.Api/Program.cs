using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using HealthNote.Api;
using HealthNote.Api.Persistence;
using HealthNote.Application.Emr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
int port = builder.Configuration.GetValue("Emr:Port", 5078);
if (port < 0 || port > 65535) throw new InvalidOperationException("Invalid loopback port.");
builder.WebHost.UseUrls("http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture));
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
string path = builder.Configuration["Emr:DatabasePath"] ?? Path.Combine(".local", "emr", "healthnote.db");
builder.Services.AddSingleton<IEmrRepository>(new SqliteEmrRepository(path));
WebApplication app = builder.Build();
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (BadHttpRequestException)
    {
        await EmrEndpoints.Problem(context, 400, "validation", "Invalid request body.").ExecuteAsync(context);
    }
    catch (SqliteException)
    {
        await EmrEndpoints.Problem(context, 503, "storage_unavailable", "Storage is temporarily unavailable.").ExecuteAsync(context);
    }
});
app.UseStatusCodePages(async status =>
{
    await EmrEndpoints.Problem(status.HttpContext, status.HttpContext.Response.StatusCode, "request_error", "Request could not be processed.")
        .ExecuteAsync(status.HttpContext);
});
app.MapGet("/api/v1/health", () => Results.Ok(new { mode = "demo", version = "v1", ready = true }));
app.MapEmr();
app.Run();
