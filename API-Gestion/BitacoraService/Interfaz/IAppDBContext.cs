using BitacoraService.Data;
using Microsoft.EntityFrameworkCore;

namespace BitacoraService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Evento> Evento { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}