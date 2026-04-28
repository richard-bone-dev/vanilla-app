using Scalar.AspNetCore;
using Vanilla.Api;
using Vanilla.Application;
using Vanilla.Infrastructure;
using Vanilla.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<LedgerApplicationService>();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();


var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/environment", (IHostEnvironment env) => new
    {
        env.EnvironmentName,
        env.ApplicationName,
        env.ContentRootPath
    });
}

app.MapOpenApi();
app.MapScalarApiReference();

app.MapLedgerApi();

app.Run();

public partial class Program;
