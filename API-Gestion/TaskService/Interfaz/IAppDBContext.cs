using Microsoft.EntityFrameworkCore;
using TaskService.Data;

namespace TaskService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Tarea> Tareas { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}