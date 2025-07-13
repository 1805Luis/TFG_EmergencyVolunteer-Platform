using EmergencyService.Data;
using EmergencyService.Interfaz;
using Microsoft.EntityFrameworkCore;

namespace EmergencyService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<Emergency> emergencies { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("EmergencyConnection");
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Emergency>()
                .Property(u => u.estado)
                .HasConversion<string>();

            modelBuilder.Entity<Emergency>()
                .Property(u => u.gravedad)
                .HasConversion<string>();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
