using Block_Chain_Example_1.Models;
using System.Security.Cryptography;
using System.Text;

namespace Block_Chain_Example_1.Services
{
    public class BlockChainService
    {
        public List<Block> Chain { get; set; } = new List<Block>();

        public int Difficulty { get; set; } = 3; // Визначає складність майнінгу (кількість провідних нулів у хеші)
        public string PrivateKeyXml { get; set; } // Зовнішній доступ до приватного ключа у форматі XML для підпису блоків поза класом
        public string PublicKeyXml { get; set; } // Зовнішній доступ до публічного ключа у форматі XML для перевірки підписів поза класом

        public Dictionary<string, Wallet> Wallets { get; set; } = new Dictionary<string, Wallet>(); // Словник гаманців за адресою
        public List<Transaction> Mempool { get; set; } = new List<Transaction>(); // Список непідтверджених транзакцій
        public const decimal MinerReward = 1.0m; // Фіксована винагорода майнеру за кожен знайдений блок


        public BlockChainService()
        {
            var rsa = RSA.Create();                     //  створюю нову пару ключів: приватний і публічний
            PrivateKeyXml = rsa.ToXmlString(true);      // Зберігаю приватний ключ для підпису блоків
            PublicKeyXml = rsa.ToXmlString(false);      // Зберігаю публічний ключ у форматі XML для зберігання в блоці

            var block = new Block(0, "");               // Створюю генезіс-блок (перший блок у ланцюжку) з індексом 0 та порожнім PrevHash
            block.Sign(PrivateKeyXml, PublicKeyXml);        // Підписую генезіс-блок за допомогою приватного ключа
            Chain.Add(block);                               // Додаю генезіс-блок до ланцюжка
        }

        public Wallet RegisterWallet(string publicKeyXml, string displayName) // Реєстрація нового гаманця
        {
            var wallet = new Wallet
            {
                PublicKeyXml = publicKeyXml,
                Address = Wallet.DereveAddressFromPublicKeyXml(publicKeyXml),
                DisplayName = displayName,
            };
            Wallets[wallet.Address] = wallet;
            return wallet;
        }

        public void CreateTransaction(Transaction transaction)      // Створення нової транзакції
        {
            var rsa = RSA.Create();
            var wallet = Wallets[transaction.FromAddress];
            rsa.FromXmlString(wallet.PublicKeyXml);
            var payload = Encoding.UTF8.GetBytes(transaction.CanonicalPayload());
            var sig = Convert.FromBase64String(transaction.Signature);
            if (!rsa.VerifyData(payload, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new Exception("Недійсний підпис транзакції"); // Якщо підпис недійсний, викидаю виключення
            Mempool.Add(transaction);                                      // Додаю транзакцію до мемпулу
        }

        public Block MinePending(string privateKey)         // Майнінг нового блоку з транзакціями з мемпулу
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);
            var publicMinerKey = rsa.ToXmlString(false);
            var minerAdress = Wallets.Values.FirstOrDefault(w => w.PublicKeyXml == publicMinerKey)?.Address;

            decimal totalFee = Mempool.Sum(t => t.Fee); // Обчислюю загальну суму комісій у мемпулі
            var baht = new List<Transaction>() {
                new Transaction()
                {
                    FromAddress = "COINBASE",
                        ToAddress = minerAdress,
                        Amount = MinerReward + totalFee,
                }
            };
            baht.AddRange(Mempool); // Додаю всі транзакції з мемпулу до блоку
            var PrevBlock = Chain[Chain.Count - 1]; // Отримую останній блок у ланцюжку
            var newBlock = new Block(Chain.Count, PrevBlock.Hash); // Створюю новий блок: індекс = порядковий номер, дані = порожні, PrevHash = хеш останнього блоку
            newBlock.SetTransaction(baht);               // Встановлюю транзакції у новий блок
            newBlock.Mine(Difficulty);                  // Генерую новий блок з заданою складністю

            newBlock.Sign(privateKey, publicMinerKey); // Підписую новий блок за допомогою приватного ключа, зберігаю публічний ключ у блоці для подальшої перевірки
            Chain.Add(newBlock);                        // Додаю новий блок до ланцюжка

            Mempool.Clear();                            // Очищую мемпул після додавання транзакцій до блоку

            return newBlock;
        }


        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)                        // Починаю перевірку з другого блоку (індекс 1)
            {
                var current = Chain[i];                                  // Поточний блок
                var prev = Chain[i - 1];                            // Попередній блок

                if (current.PrevHash != prev.Hash) return false;    // Перевірка відповідності хешів
                if (current.Hash != current.ComputeHash()) return false; // Перевірка валідності хешу
                if (!current.Verify()) return false;                     // Перевірка валідності підпису
                if(!current.HashValidProof()) return false;               // Перевірка відповідності хешу вимогам складності
            }
            return true;
        }

        public (Wallet wallet, string privateKeyXml) CreateWallet(string displayName)   // Створення нового гаманця з парою ключів
        {
            var rsa = RSA.Create();                             // Створюю нову пару ключів для гаманця
            var privateKeyXml = rsa.ToXmlString(true);         // Експортую приватний ключ у форматі XML
            var publicKeyXml = rsa.ToXmlString(false);         // Експортую публічний ключ у форматі XML
            var wallet = RegisterWallet(publicKeyXml, displayName); // Реєструю гаманець з публічним ключем і відображуваним ім'ям
            return (wallet, privateKeyXml); // Повертаю гаманець і приватний ключ   
        }


        public static string SignPayload(string payload, string privateKeyXml)      // Підписати довільний текстовий рядок за допомогою приватного ключа у форматі XML
        {
            var rsa = RSA.Create();                         // Створюю новий екземпляр RSA для підпису
            rsa.FromXmlString(privateKeyXml);               // Імпортую приватний ключ з XML
            var data = Encoding.UTF8.GetBytes(payload);     // Конвертую текстовий рядок у байти
            var sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);  // Підписую дані з використанням SHA256
            return Convert.ToBase64String(sig);             // Повертаю підпис у форматі Base64
        }



        public int? GetFirstInvalidIndex()      // Пошук індексу першого невалідного блоку
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var current = Chain[i];
                var prevBlock = Chain[i - 1];

                bool hashIsMatch = current.PrevHash == prevBlock.Hash;    // Перевірка відповідності хешів
                bool hashIsValid = current.Hash == current.ComputeHash(); // Перевірка валідності хешу
                bool signatureIsValid = current.Verify();                 // Перевірка валідності підпису

                if (!hashIsMatch || !hashIsValid || !signatureIsValid)
                    return i;
            }

            return null;
        }

        public Block FindBlock(string query)  // Пошук блоку за хешем або індексом
        {
            if (int.TryParse(query, out int index))
            {
                return Chain.FirstOrDefault(b => b.Index == index);
            }
            else
            {
                return Chain.FirstOrDefault(b => b.Hash.Equals(query, StringComparison.OrdinalIgnoreCase));
            }
        }

    }
}
