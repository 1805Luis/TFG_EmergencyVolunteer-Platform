using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UserService.Custom
{
    public class Utilidades
    {
        private readonly IConfiguration _configuration;
        public Utilidades(IConfiguration configuration)
        {
            _configuration = configuration;
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
    }
}
