using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Custom
{
    public class Utilidades
    {
        private readonly IConfiguration _configuration;
        public Utilidades(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarSalt(int size = 16)
        {
            byte[] saltBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        // Hashea la combinación input + salt usando SHA256
        public string HashConSalt(string input, string salt)
        {
            string inputConSalt = input + salt;
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputConSalt));
            StringBuilder sb = new();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // Verifica que el input, con la sal dada, genera el hash esperado


        public string HashConSaltOpcional(string input, string? salt = null)
        {
            string inputFinal = string.IsNullOrEmpty(salt) ? input : input + salt;

            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputFinal));

            StringBuilder sb = new();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public bool VerificarHash(string input, string? salt, string hashEsperado)
        {
            string hashCalculado = HashConSaltOpcional(input, salt);
            return string.Equals(hashCalculado, hashEsperado, StringComparison.OrdinalIgnoreCase);
        }




        public string generarJWT(TokenDTO token, int minutos)
        {
            var userClaim = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,token.Id.ToString()),
                new Claim(ClaimTypes.Role,token.Rol)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            //crear detalle del token
            var jwtConfig = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: userClaim,
                signingCredentials: credentials,
                expires: DateTime.UtcNow.AddMinutes(minutos)
                );

            return new JwtSecurityTokenHandler().WriteToken(jwtConfig);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Token aleatorio seguro
        }

        // Método para cifrar con AES
        public string CifrarAES(string textoPlano)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["AES:Key"]!);
            var iv = Encoding.UTF8.GetBytes(_configuration["AES:IV"]!);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(textoPlano);
            sw.Close();

            return Convert.ToBase64String(ms.ToArray());
        }

        //Método para descifrar con AES
        public string DescifrarAES(string textoCifrado)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["AES:Key"]!);
            var iv = Encoding.UTF8.GetBytes(_configuration["AES:IV"]!);

            var buffer = Convert.FromBase64String(textoCifrado);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

    }
}
