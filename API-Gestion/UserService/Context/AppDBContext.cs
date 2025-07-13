using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Interfaz;

namespace UserService.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {
        public DbSet<User> User { get; set; }

        private readonly IConfiguration _configuration;

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("UserConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
