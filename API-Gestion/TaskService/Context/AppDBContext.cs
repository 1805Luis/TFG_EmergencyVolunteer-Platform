using Microsoft.EntityFrameworkCore;
using TaskService.Data;
using TaskService.Interfaz;

namespace TaskService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<Tarea> Tareas { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("TaskConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tarea>()
                .Property(u => u.estado)
                .HasConversion<string>();

            modelBuilder.Entity<Tarea>()
                .Property(u => u.gravedad)
                .HasConversion<string>();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
