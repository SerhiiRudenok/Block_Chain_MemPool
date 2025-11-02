using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

namespace Block_Chain_Example_1.Models
{
    public class Block
    {
        [Key]
        public int Index { get; set; }
        public List<Transaction> Transactions { get; } = new(); // Список транзакцій у блоці з найбльшою комісією
        public int TxCount => Transactions.Count; // Кількість транзакцій у блоці
        public DateTime Timestamp { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        
        public string Signature { get; set; }
        public string PublicKey { get; private set; }

        // POW
        public int Nonce { get; set; }              // Число, яке використовується для пошуку валідного хешу
        public int Difficulty { get; set; }         // Визначає складність пошуку валідного хешу: кількість нулів на початку блоку
        public long MiningDurationMs { get; set; }  // Час, витрачений на майнінг блоку в мілісекундах


        public Block() { }  // Конструктор без параметрів
        public Block(int index, string prevHash)
        {
            Index = index;
            PrevHash = prevHash;
            Timestamp = DateTime.UtcNow;
            Hash = ComputeHash();
        }

        public Block(int index, string prevHash, DateTime timestamp, string hash)
        {
            Index = index;
            PrevHash = prevHash;
            Timestamp = timestamp;
            Hash = hash;
        }

        public void SetTransaction(List<Transaction> transactions)  // Встановити список транзакцій у блок
        {
            Transactions.Clear();
            Transactions.AddRange(transactions);
        }

        private string CononicalizetTransactions() // Отримати канонічний текстовий вигляд усіх транзакцій у блоці
        {
            var sb = new StringBuilder();
            foreach (var tx in Transactions)
            {
                sb.Append(tx.CanonicalPayload());
                sb.Append("|");
            }
            return sb.ToString();
        }

        public string ComputeHash()
        {
            var raw = Index + PrevHash + Timestamp + Nonce + Difficulty + CononicalizetTransactions();     // Комбіную всі властивості блоку в один рядок
            using (var sha = SHA256.Create())                                       // Використовую SHA256 для обчислення хешу
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));        // Обчислюю хеш у вигляді масиву байтів
                return BitConverter.ToString(bytes).Replace("-", "");               // Перетворюю байти в шістнадцятковий рядок без дефісів
            }
        }

        public void Sign(string privateKeyXml, string publicKeyXml) // Підписати блок за допомогою приватного ключа
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);

            byte[] data = Encoding.UTF8.GetBytes(Hash);                                           // Підписую хеш блоку
            byte[] sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); // Використовую SHA256 для хешування при підписі
            
            Signature = Convert.ToBase64String(sig);                                              // Підпис у форматі Base64
            PublicKey = publicKeyXml;
        }

        public bool Verify() // Перевірити підпис блоку за допомогою публічного ключа
        {
            var rsa = RSA.Create();                             // Створюю новий екземпляр RSA для перевірки підпису
            rsa.FromXmlString(PublicKey);                       // Імпортую публічний ключ з XML

            var data = Encoding.UTF8.GetBytes(Hash);         // Отримую байти хешу блоку
            var sign = Convert.FromBase64String(Signature);   // Декодую підпис з Base64
            return rsa.VerifyData(data, sign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); // Перевіряю підпис за допомогою SHA256
        }

        public void Mine(int difficulty)                // Генерую хеш, до тих пір поки він не відповідатиме вимогам складності
        {
            Difficulty = difficulty;
            string target = new string('0', Difficulty); // Унікальна строка з необхідною кількістю нулів

            var sw = Stopwatch.StartNew();               // Вимірюю час майнінгу
            do
            {
                Nonce++;                                // Збільшую число Nonce для зміни хешу
                Hash = ComputeHash();                   // Обчислюю новий хеш з оновленим Nonce
            } while (!Hash.StartsWith(target, StringComparison.Ordinal)); // Перевіряю, чи починається хеш з необхідної кількості нулів

            sw.Stop();                                  // Зупиняю таймер
            MiningDurationMs = sw.ElapsedMilliseconds;  // Записую час майнінгу в мілісекундах
        }

        public bool HashValidProof()    // Перевіряю, чи відповідає хеш вимогам складності
        {
            string target = new string('0', Difficulty); // Унікальна строка з необхідною кількістю нулів
            return Hash == ComputeHash() && Hash.StartsWith(target, StringComparison.Ordinal); // Перевіряю відповідність хешу та вимогам складності
        }

    }
}

