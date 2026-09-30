using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// The foundation host only serves readiness on loopback; no health data or writes.
builder.WebHost.UseUrls("http://127.0.0.1:5078");
WebApplication app = builder.Build();
app.MapGet("/api/v1/health", () => Results.Ok(new { mode = "demo", version = "v1", ready = true }));
app.Run();
