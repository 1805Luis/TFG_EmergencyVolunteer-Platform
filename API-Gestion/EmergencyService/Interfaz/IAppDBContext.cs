using EmergencyService.Data;
using Microsoft.EntityFrameworkCore;

namespace EmergencyService.Interfaz
{
    public interface IAppDBContext
    {
        DbSet<Emergency> emergencies { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}