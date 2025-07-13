using GroupService.Data;
using GroupService.Interfaz;
using Microsoft.EntityFrameworkCore;

namespace GroupService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<Grupo> grupos { get; set; }

        public DbSet<Recursos> recursos { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("GroupConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }

    }
}
