using DotNet.JwtWebApi;
using DotNet.JwtWebApi.Registration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", false, true);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.SetupDatabase();

builder.SetupIdentityCore();

builder.AddCorsPolicy();


builder.SetupJwtAuthentication();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors("CorsPolicy");

app.Run();