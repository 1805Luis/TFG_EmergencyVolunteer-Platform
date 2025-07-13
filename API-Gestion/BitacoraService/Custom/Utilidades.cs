using System.Security.Cryptography;
using System.Text;

namespace BitacoraService.Custom
{
    public class Utilidades
    {
        private readonly IConfiguration _configuration;
        public Utilidades(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
