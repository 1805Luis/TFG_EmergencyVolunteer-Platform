using System.Text.Json;
using BitacoraService.Data;
using BitacoraService.Interfaz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BitacoraService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<Evento> Evento { get; set; }
        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("BitacoraConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Evento>(entity =>
            {
                entity.Property(e => e.Action)
                      .HasConversion<string>() // Guarda el enum como texto
                      .IsRequired();

                entity.Property(e => e.Timestamp)
                      .HasDefaultValueSql("NOW()");

                entity.Property(e => e.Payload)
                      .HasColumnType("jsonb") // Mapear como jsonb para PostgreSQL
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                          v => JsonSerializer.Deserialize<Payload>(v, (JsonSerializerOptions)null)
                      );
            });
        }



        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }

    }
}
