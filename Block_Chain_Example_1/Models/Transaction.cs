using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Block_Chain_Example_1.Models
{
    public class Transaction
    {
        [Required]
        public string FromAddress { get; set; } = string.Empty;     // Адреса відправника

        [Required]
        public string ToAddress { get; set; } = string.Empty;       // Адреса отримувача

        public decimal Amount { get; set; }         // Сума транзакції
        public decimal Fee { get; set; }            // Комісія за транзакцію
        public string Signature { get; set; } = string.Empty; // Підпис транзакції

        public string? Note { get; set; }               // Додаткова примітка до транзакції

        public string CanonicalPayload()        // Текстовий вигляд транзакції для підпису та перевірки
        {
            return string.Format(CultureInfo.InvariantCulture, "From:{0}|To:{1}|Amount:{2:0.########}|Fee:{3:0.########}",
                FromAddress,
                ToAddress,
                Amount,
                Fee);
        }
    }
}
