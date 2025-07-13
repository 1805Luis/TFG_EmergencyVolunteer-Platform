using AssigmentService.Data;
using Microsoft.EntityFrameworkCore;

namespace AssigmentService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Grupos_Voluntarios> grupos_voluntarios { get; set; }
        DbSet<Tareas_Grupos> tareas_grupos { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}