using API.Cors;
using API.Errors;
using API.RateLimiting;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddApiControllers();
builder.Services.AddApiProblemDetails();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(CorsExtensions.FrontendPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
