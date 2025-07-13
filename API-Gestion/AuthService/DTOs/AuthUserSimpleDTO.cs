namespace AuthService.DTOs
{
    public class AuthUserSimpleDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
    }
}
