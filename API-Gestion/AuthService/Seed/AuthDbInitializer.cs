using AuthService.Context;
using AuthService.Custom;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Service;

namespace AuthService.Seed
{
    public class AuthDbInitializer
    {
        public static void SeedAdmin(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
                var utils = scope.ServiceProvider.GetRequiredService<Utilidades>();
                var rabbit = scope.ServiceProvider.GetRequiredService<RabbitMQProducer>();

                if (!db.Auth_User.Any(u => u.Rol == Rol_User.Security_Ops))
                {
                    var adminId = Guid.NewGuid();
                    var email = "admin@voluntarios.com";
                    var password = "Admin123!";

                    var salt = utils.GenerarSalt();
                    var hashConSalt = utils.HashConSaltOpcional(password, salt);

                    var adminUser = new Auth_User
                    {
                        Id = adminId,
                        Email = utils.CifrarAES(email),
                        ClaveHash = hashConSalt,
                        ClaveSalt = salt,
                        Rol = Rol_User.Security_Ops,
                        Estado = Estado_User.activo
                    };

                    db.Auth_User.Add(adminUser);
                    db.SaveChanges();

                    var userDto = new UserDTO
                    {
                        Id = adminId,
                        Name = "Administrador del sistema",
                        Telf = "000000000",
                        Disponibilidad = "Siempre",
                        Habilidades = "Gestión"
                    };

                    rabbit.SendMessageToQueueAsync("user_create_queue", userDto, "user_ServiceConnection",false).Wait();
                    Console.WriteLine("✅ Administrador creado correctamente");
                }
                else
                {
                    Console.WriteLine("ℹ Ya existe un administrador registrado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error al ejecutar el seeder de admin:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine($"↪ Inner: {ex.InnerException.Message}");
            }
        }
    }
}
