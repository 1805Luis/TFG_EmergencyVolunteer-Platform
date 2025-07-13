using System.Text;
using System.Text.Json.Serialization;
using BitacoraService.Context;
using BitacoraService.Custom;
using BitacoraService.Interfaz;
using BitacoraService.Middleware;
using BitacoraService.Service;
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("BitacoraConnection")));


builder.Services.AddAuthentication(config =>
{
    // Indica que se usará JWT (tokens) como el sistema principal para comprobar si un usuario está autenticado
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // Indica que, si un usuario intenta acceder sin estar autenticado, el sistema usará JWT para manejar la respuesta
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Agrega el esquema de autenticación JWT
.AddJwtBearer(config =>
{
    // Indica si se requiere HTTPS para los tokens. Se pone en "false" para entornos de desarrollo,pero en producción debe ser "true" por seguridad.
    config.RequireHttpsMetadata = false;

    // Guarda el token en la solicitud autenticada para reutilizarlo si es necesario
    config.SaveToken = true;

    // Define los parámetros de validación del token
    config.TokenValidationParameters = new TokenValidationParameters
    {
        // Habilita la validación de la clave de firma del emisor del token
        ValidateIssuerSigningKey = true,

        // Valida el emisor del token.
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        // Valida la audiencia del token.
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // Habilita la validación de la vida útil del token (expiración)
        ValidateLifetime = true,

        // Define la tolerancia de tiempo en la expiración del token. Se establece en "0" para que no haya margen de error.
        ClockSkew = TimeSpan.Zero,

        // Define la clave secreta con la que se firmaron los tokens. Esta clave debe coincidir con la utilizada para firmar los tokens en el servidor.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]!))
    };
});


// Configurar settings de RabbitMQ
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

// Registrar servicio consumidor
builder.Services.AddHostedService<RabbitMQConsumer>();

builder.Services.AddSingleton<Utilidades>();

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

