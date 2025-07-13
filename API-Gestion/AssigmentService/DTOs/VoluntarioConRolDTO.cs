using AssigmentService.Data;

namespace AssigmentService.DTOs
{
    public class VoluntarioConRolDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public Rol Rol { get; set; }
        public bool EsMiUsuario { get; set; }
    }
}
