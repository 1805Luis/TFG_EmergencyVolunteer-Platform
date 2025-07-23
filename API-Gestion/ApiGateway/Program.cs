using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Agregamos configuraci�n desde ocelot.json y variables de entorno
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
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
    var id = Environment.GetEnvironmentVariable("GATEWAY_ID") ?? "desconocido";
    Console.WriteLine($"Recibí una petición en {id} - {context.Request.Method} {context.Request.Path}");
    await next();
});


await app.UseOcelot();

app.Run();
