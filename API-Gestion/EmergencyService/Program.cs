using System.Text;
using System.Text.Json.Serialization;
using EmergencyService.Context;
using EmergencyService.Interfaz;
using EmergencyService.Middleware;
using EmergencyService.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IAppDBContext, AppDBContext>();
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("EmergencyConnection"),
        o => o.UseNetTopologySuite()
    ));


builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMQProducer>();


builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(config =>
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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseAuthorization();

app.MapControllers();

var scope = app.Services.CreateScope();

await Migrations(scope.ServiceProvider);

app.Run();



async Task Migrations(IServiceProvider services)
{
    const int maxRetries = 5;
    const int delayIncrement = 5; // Aumenta 5s por intento
    const int maxDelaySeconds = 30;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        Console.WriteLine($"\n========== INICIANDO MIGRACIONES - Intento {attempt} ==========");

        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();

            Console.WriteLine("Aplicando migraciones pendientes...");
            await context.Database.MigrateAsync();

            Console.WriteLine("Migraciones aplicadas correctamente.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en intento {attempt}: {ex.Message}");

            if (attempt < maxRetries)
            {
                int delay = Math.Min(attempt * delayIncrement, maxDelaySeconds);
                Console.WriteLine($" Esperando {delay} segundos antes de reintentar...");
                await Task.Delay(delay * 1000);
            }
            else
            {
                Console.WriteLine(" Número máximo de intentos alcanzado. Abortando migraciones.");
            }
        }
    }

    Console.WriteLine("========== MIGRACIONES FINALIZADAS ==========\n");
}
