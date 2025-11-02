using System.Security.Cryptography;
using System.Text;

namespace Block_Chain_Example_1.Models
{
    public class Wallet
    {
        public string Address { get; set; } = string.Empty; // Адреса гаманця у форматі Base64
        public string PublicKeyXml { get; set; } = string.Empty; // Публічний ключ у форматі XML
        public string DisplayName { get; set; } = string.Empty; // Відображуване ім'я гаманця
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow; // Дата та час реєстрації гаманця

        public static string DereveAddressFromPublicKeyXml(string publicKeyXml)
        {
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(publicKeyXml));
            var hex20 = BitConverter.ToString(hash, 0, 20).Replace("-", ""); // Використовую перші 20 байтів хешу
            return "ADDR_" + hex20; // Формую адресу гаманця з префіксом "ADDR_"
        }
    }
}
