using FinGrid.Models;
using FinGrid.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinGrid.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BankController : ControllerBase
    {
        private readonly MonobankService _monoService;
        private readonly ApplicationDbContext _context; // Твій DbContext

        public BankController(MonobankService monoService, ApplicationDbContext context)
        {
            _monoService = monoService;
            _context = context;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncMonobank([FromBody] string publicToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var monoInfo = await _monoService.GetClientInfoAsync(publicToken);

            if (monoInfo == null) return BadRequest("Не вдалося отримати дані.");

            // 1. Оновлюємо зв'язок (BankConnection) - як ми робили раніше
            var connection = await _context.BankConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection == null)
            {
                _context.BankConnections.Add(new BankConnection { UserId = userId, PublicToken = publicToken, LastSync = DateTime.UtcNow });
            }
            else
            {
                connection.PublicToken = publicToken;
                connection.LastSync = DateTime.UtcNow;
            }

            // 2. СИНХРОНІЗАЦІЯ РАХУНКІВ (BankAccounts)
            foreach (var accDto in monoInfo.Accounts)
            {
                // Шукаємо, чи цей рахунок уже є в нашій базі
                var existingAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ExternalId == accDto.Id);

                if (existingAccount != null)
                {
                    // Оновлюємо існуючий
                    existingAccount.Balance = accDto.Balance / 100.0m; // Конвертуємо копійки в грн
                    existingAccount.LastUpdated = DateTime.UtcNow;
                    existingAccount.Type = accDto.Type;
                    existingAccount.Iban = accDto.Iban;
                }
                else
                {
                    // Створюємо новий
                    var newAccount = new BankAccount
                    {
                        UserId = userId,
                        ExternalId = accDto.Id,
                        Balance = accDto.Balance / 100.0m,
                        CurrencyCode = accDto.CurrencyCode,
                        Type = accDto.Type,
                        Iban = accDto.Iban,
                        LastUpdated = DateTime.UtcNow,
                        Name = GetAccountNameByType(accDto.Type) // Допоміжна функція нижче
                    };
                    _context.BankAccounts.Add(newAccount);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Синхронізація завершена", accounts = monoInfo.Accounts.Count });
        }

        // Маленький хелпер для назв
        private string GetAccountNameByType(string type) => type switch
        {
            "black" => "Чорна картка",
            "white" => "Біла картка",
            "fop" => "ФОП рахунок",
            "platinum" => "Платинова картка",
            _ => "Рахунок Monobank"
        };


        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            // Отримуємо ID поточного користувача з токена
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Дістаємо всі його рахунки, які ми щойно зберегли
            var accounts = await _context.BankAccounts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Balance) // Найбагатші зверху :)
                .ToListAsync();

            return Ok(accounts);
        }


        [HttpPost("sync-transactions/{accountId}")]
        public async Task<IActionResult> SyncTransactions(string accountId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var connection = await _context.BankConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            var bankAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.ExternalId == accountId && a.UserId == userId);

            if (connection == null || bankAccount == null) return BadRequest("Рахунок не знайдено");

            var statements = await _monoService.GetStatementsAsync(connection.PublicToken, accountId);

            if (statements != null)
            {
                foreach (var item in statements)
                {
                    // Перевіряємо, чи ми вже не додавали цю транзакцію раніше
                    if (!await _context.BankTransactions.AnyAsync(t => t.ExternalId == item.Id))
                    {
                        _context.BankTransactions.Add(new BankTransaction
                        {
                            BankAccountId = bankAccount.Id,
                            ExternalId = item.Id,
                            Time = item.Time,
                            Description = item.Description,
                            Mcc = item.Mcc,
                            Amount = item.Amount / 100.0m,
                            BalanceAfter = item.Balance / 100.0m
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Історію оновлено" });
        }
        // 1. Отримати виписку для конкретного рахунку (за externalId)
        [HttpGet("transactions/{accountId}")]
        public async Task<IActionResult> GetTransactionsByAccount(string accountId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var transactions = await _context.BankTransactions
                .Include(t => t.BankAccount)
                .Where(t => t.BankAccount.ExternalId == accountId && t.BankAccount.UserId == userId)
                .OrderByDescending(t => t.Time) // Найсвіжіші зверху
                .ToListAsync();

            return Ok(transactions);
        }

        // 2. Отримати загальну історію всіх витрат користувача
        [HttpGet("transactions-all")]
        public async Task<IActionResult> GetAllUserTransactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var transactions = await _context.BankTransactions
                .Include(t => t.BankAccount)
                .Where(t => t.BankAccount.UserId == userId)
                .OrderByDescending(t => t.Time)
                .Take(100) // Обмежимо останньою сотнею для швидкості
                .ToListAsync();

            return Ok(transactions);
        }
    }
}