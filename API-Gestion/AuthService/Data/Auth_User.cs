using AuthService.Data;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Data
{
    public class Auth_User
    {
        public Guid Id { get; set; }

        [MaxLength(256)]
        public string Email { get; set; }

        [MaxLength(256)]
        public string ClaveHash { get; set; }  

        [MaxLength(128)]
        public string ClaveSalt { get; set; }

        public Rol_User Rol { get; set; }

        public Estado_User Estado { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}


