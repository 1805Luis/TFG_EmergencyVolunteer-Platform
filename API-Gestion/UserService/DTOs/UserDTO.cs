using UserService.Data;

namespace UserService.DTOs
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Telf { get; set; }
        public string? Disponibilidad { get; set; }
        public string? Habilidades { get; set; }

        public UserDTO() { }
        public UserDTO(User user)
        {
            Id = user.Id;
            Name = user.Name;
            Telf = user.Telf;
            Disponibilidad = user.Disponibilidad;
            Habilidades = user.Habilidades;
        }

    }
}
