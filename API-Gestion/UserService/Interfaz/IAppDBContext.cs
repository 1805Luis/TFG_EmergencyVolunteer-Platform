using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<User> User { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}