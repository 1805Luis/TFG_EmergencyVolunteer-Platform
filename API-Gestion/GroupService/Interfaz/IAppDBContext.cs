using GroupService.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Grupo> grupos { get; set; }
        DbSet<Recursos> recursos { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}