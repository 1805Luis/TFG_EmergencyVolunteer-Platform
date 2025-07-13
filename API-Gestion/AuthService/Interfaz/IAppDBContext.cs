using AuthService.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Auth_User> Auth_User { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}