using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var env = Environment.GetEnvironmentVariable("OCEL0T_ENV") ?? "dev";
var configFile = $"ocelot.{env}.json";

// Mensaje informativo en consola
Console.WriteLine($" Cargando configuración Ocelot desde: {configFile}");

builder.Configuration
    .AddJsonFile(configFile, optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});



// Registramos Ocelot
builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("JwtBearer", config =>
{
    config.RequireHttpsMetadata = false;

    config.SaveToken = true;

    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ValidateLifetime = true,

        ClockSkew = TimeSpan.Zero,

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]!))
    };
});

var app = builder.Build();

var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID") ?? "desconocido";


app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    Console.WriteLine($"📡 [{gatewayId}] - {context.Request.Method} {context.Request.Path}");
    await next();
});


await app.UseOcelot();

Console.WriteLine($"🚀 Gateway [{gatewayId}] iniciado y esperando peticiones...");

app.Run();
