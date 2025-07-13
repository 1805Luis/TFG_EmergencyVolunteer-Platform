using AuthService.Data;
using AuthService.Interfaz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthService.Context
{
    // Esta clase representa el contexto de base de datos y hereda de DbContext
    // Además implementa la interfaz IAppDBContext para permitir la inyección de dependencias
    public class AppDBContext : DbContext, IAppDBContext
    {
 
        
        private readonly IConfiguration _configuration; // Acceso a la configuración para obtener la cadena de conexión

        // Define el conjunto de datos (tabla) Auth_User que EF Core mapeará a una tabla SQL
        public DbSet<Auth_User> Auth_User { get; set; }

        // Constructor que recibe las opciones de contexto y la configuración
        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        // Configura el proveedor de base de datos (en este caso SQL Server) con la cadena de conexión
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("AuthConnection"); // Obtiene la cadena desde appsettings.json
            optionsBuilder.UseSqlServer(connectionString); // Establece que se usará SQL Server como base de datos
        }

        // Configura el modelo de datos antes de que se creen las tablas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Llama a la configuración base

            // Configura que el campo 'Rol' del modelo Auth_User, que es un enum, se guarde como string en la base de datos
            modelBuilder.Entity<Auth_User>()
                .Property(u => u.Rol)
                .HasConversion<string>(); // Guarda "Voluntario", "Security_Ops" en lugar de 0, 1

            modelBuilder.Entity<Auth_User>()
                .Property(u => u.Estado)
                .HasConversion<string>(); // Guarda "Voluntario", "Security_Ops" en lugar de 0, 1
        }

        // Método sobrescrito que permite guardar los cambios de forma asíncrona
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }

}

