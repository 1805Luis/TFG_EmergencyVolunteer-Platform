namespace AuthService.DTOs
{
    public class UserRegistrationDto
    {
        public string Correo { get; set; }
        public string Clave { get; set; }
        public string Nombre { get; set; }
        public string? Telf { get; set; }
        public string? Disponibilidad { get; set; }
        public string? Habilidades { get; set; }
        
    }
}
