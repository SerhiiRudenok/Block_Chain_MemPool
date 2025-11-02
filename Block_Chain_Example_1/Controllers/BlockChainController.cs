using Block_Chain_Example_1.Hubs;
using Block_Chain_Example_1.Models;
using Block_Chain_Example_1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;


namespace Block_Chain_Example_1.Controllers
{
    public class BlockChainController : Controller
    {
        private static CancellationTokenSource _miningCts;

        private static BlockChainService blockChainService = new BlockChainService();

        // Index
        public IActionResult Index()
        {
            ViewBag.Valid = blockChainService.IsValid();                           // Перевірити валідність всього блокчейну
            ViewBag.FirstInvalidIndex = blockChainService.GetFirstInvalidIndex();  // Отримати індекс першого невалідного блоку
            ViewBag.Difficulty = blockChainService.Difficulty;                     // Поточна складність майнінгу
            ViewBag.PrivateKey = blockChainService.PrivateKeyXml;                  // Приватний ключ майнера у форматі XML
            ViewBag.PublicKey = blockChainService.PublicKeyXml;                    // Публічний ключ майнера у форматі XML
            ViewBag.MempoolCount = blockChainService.Mempool.Count;                 // Кількість транзакцій у мемпулі
            ViewBag.Mempool = blockChainService.Mempool;                            // Список транзакцій у мемпулі
            ViewBag.Wallets = blockChainService.Wallets.Values.ToList();            // Список зареєстрованих гаманців
            ViewBag.HasMinedBlock = TempData["HasMinedBlock"] as bool? ?? false;     // Чи є збережені результати майнінгу для додавання блоку
            return View(blockChainService.Chain);
        }



        [HttpPost]
        public IActionResult RegisterWallet(string PublicKeyXml, string displayName)    // Реєстрація нового гаманця
        {
            var wallet = blockChainService.RegisterWallet(PublicKeyXml, displayName);
            return RedirectToAction("Index");
        }


        // Створення нової транзакції
        [HttpPost]
        public IActionResult CreateTransaction(string fromAddress, string toAddress, decimal amount, decimal fee, string privateKey, string note)
        {
            var tx = new Models.Transaction
            {
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Amount = amount,
                Fee = fee,
                Note = note
            };
            tx.Signature = BlockChainService.SignPayload(tx.CanonicalPayload(), privateKey);
            try
            {
                blockChainService.CreateTransaction(tx);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }


        //Функція майнінгу
        [HttpPost]
        public IActionResult MinePending(string privateKey)     // Майнінг нового блоку з транзакціями з мемпулу
        {
            try
            {
                blockChainService.MinePending(privateKey);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult DemoSetup()
        {
            var (Ivan, prvKey1) = blockChainService.CreateWallet("Ivan");
            var (Taras, prvKey2) = blockChainService.CreateWallet("Taras");

            decimal amount = 10.0m;
            decimal fee = 0.5m;

            var tx = new Models.Transaction
            {
                FromAddress = Ivan.Address,
                ToAddress = Taras.Address,
                Amount = amount,
                Fee = fee,
                Note = "Payment for services"
            };

            var sig = BlockChainService.SignPayload(tx.CanonicalPayload(), prvKey1);

            tx.Signature = sig;

            blockChainService.CreateTransaction(tx);

            return RedirectToAction("Index");
        }


        // Зупинка майнінгу блоку
        [HttpPost]
        public IActionResult CancelMining()
        {
            _miningCts?.Cancel(); // встановлює ct.IsCancellationRequested = true
            TempData["HasMinedBlock"] = false;
            TempData.Clear();
            return RedirectToAction("Index");
        }


        // GET: BlockChainController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BlockChainController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: BlockChainController/Edit/5
        public ActionResult Edit(int id)
        {
            var block = blockChainService.Chain.FirstOrDefault(b => b.Index == id);
            if (block == null)
                return NotFound();

            return View(block);
        }

        // POST: BlockChainController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Block_Chain_Example_1.Models.Block updatedBlock)
        {
            var block = blockChainService.Chain.FirstOrDefault(b => b.Index == id);
            if (block == null)
                return NotFound();

            block.Signature = updatedBlock.Signature; // оновити підпис
            block.Hash = updatedBlock.Hash;           // оновити хеш

            ViewBag.Valid = blockChainService.IsValid();

            return RedirectToAction(nameof(Index));
        }

        // Пошук блоку за індексом або хешем
        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Search(string query)
        {
            var block = blockChainService.FindBlock(query);
            ViewBag.FoundBlock = block;
            ViewBag.IsPost = true;
            return View();
        }


        // Генерація нового приватного ключа RSA-ключа
        [HttpPost]
        public IActionResult GenerateKey()
        {
            // Генерація RSA-ключа
            using var rsa = RSA.Create(512);
            byte[] privateKeyBytes = rsa.ExportRSAPrivateKey();
            string base64Key = Convert.ToBase64String(privateKeyBytes);

            ViewBag.GeneratedKey = base64Key;
            ViewBag.Difficulty = blockChainService.Difficulty; // коли генерується ключ потрібно зберегти складність майнінгу

            // Повернення на Index.cshtml
            return View("Index", blockChainService.Chain);
        }

        [HttpPost]
        public IActionResult SetDifficulty(int difficulty) { 
            if(difficulty < 1) difficulty = 1;
            if(difficulty > 10) difficulty = 10;
            blockChainService.Difficulty = difficulty;
            return RedirectToAction("Index");
        }

    }
}
