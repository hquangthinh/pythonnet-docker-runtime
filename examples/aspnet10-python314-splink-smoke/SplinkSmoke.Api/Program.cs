using Scalar.AspNetCore;
using SplinkSmoke.Api.Endpoints;
using SplinkSmoke.Api.Python;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Single engine instance, started by the host before the app accepts requests.
builder.Services.AddSingleton<PythonEngineHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PythonEngineHost>());
builder.Services.AddSingleton<PythonSmokeTestRunner>();

var app = builder.Build();

// OpenAPI + Scalar are always on: exploring the API is the point of this example.
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Splink Smoke API"));
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.MapSmokeTestEndpoints();

app.Run();
