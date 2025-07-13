using AssigmentService.Data;
using AssigmentService.Interfaz;
using Microsoft.EntityFrameworkCore;

namespace AssigmentService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<Tareas_Grupos> tareas_grupos { get; set; }
        public DbSet<Grupos_Voluntarios> grupos_voluntarios { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("AssignConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tareas_Grupos>()
            .HasKey(e => new { e.tarea_id, e.grupo_id });

            modelBuilder.Entity<Tareas_Grupos>()
                .Property(u => u.estado)
                .HasConversion<string>();

            modelBuilder.Entity<Grupos_Voluntarios>()
            .HasKey(e => new { e.voluntario_id, e.grupo_id });

            modelBuilder.Entity<Grupos_Voluntarios>()
                .Property(u => u.rol)
                .HasConversion<string>();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
