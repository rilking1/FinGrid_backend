using FinGrid.DTO;
using FinGrid.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinGrid.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BudgetController(ApplicationDbContext context) => _context = context;

        [HttpGet("summary")]
        public async Task<IActionResult> GetBudgetSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            long unixStartOfMonth = ((DateTimeOffset)startOfMonth).ToUnixTimeSeconds();

            // 1. АВТО-СТВОРЕННЯ ГАМАНЦЯ
            // Якщо у юзера немає жодного ручного гаманця, створюємо "Готівку" за замовчуванням
            var manualWallets = await _context.ManualWallets.Where(w => w.UserId == userId).ToListAsync();
            if (!manualWallets.Any())
            {
                var defaultWallet = new ManualWallet { Name = "Готівка", Balance = 0, UserId = userId };
                _context.ManualWallets.Add(defaultWallet);
                await _context.SaveChangesAsync();
                manualWallets.Add(defaultWallet);
            }

            // 2. БАЛАНСИ
            var bankBalance = await _context.BankAccounts
                .Where(a => a.UserId == userId && a.IsIncludedInTotal)
                .SumAsync(a => a.Balance);

            var manualBalance = manualWallets.Sum(w => w.Balance);

            // 3. КАТЕГОРІЇ ТА ВИТРАТИ
            var mappings = await _context.MccMappings.Where(m => m.UserId == userId).ToListAsync();
            var categories = await _context.BudgetCategories.Where(c => c.UserId == userId).ToListAsync();
            if (!categories.Any())
            {
                var defaultCategories = new List<BudgetCategory>
        {
            new BudgetCategory { Name = "Продукти", MonthlyLimit = 5000, Icon = "cart.fill", UserId = userId },
            new BudgetCategory { Name = "Транспорт", MonthlyLimit = 1500, Icon = "car.fill", UserId = userId },
            new BudgetCategory { Name = "Розваги", MonthlyLimit = 2000, Icon = "gamecontroller.fill", UserId = userId }
        };
                _context.BudgetCategories.AddRange(defaultCategories);
                await _context.SaveChangesAsync();
                categories = defaultCategories; // Оновлюємо локальний список
            }
            var resultCategories = new List<object>();

            foreach (var cat in categories)
            {
                var assignedMccs = mappings.Where(m => m.CategoryId == cat.Id).Select(m => m.Mcc).ToList();

                var bankSpent = await _context.BankTransactions
                    .Where(t => t.BankAccount.UserId == userId &&
                                assignedMccs.Contains(t.Mcc) &&
                                t.Amount < 0 &&
                                t.Time >= unixStartOfMonth)
                    .SumAsync(t => Math.Abs(t.Amount));

                var manualSpent = await _context.ManualTransactions
                    .Where(t => t.UserId == userId &&
                                t.CategoryId == cat.Id &&
                                t.Amount < 0 &&
                                t.Date >= startOfMonth)
                    .SumAsync(t => Math.Abs(t.Amount));

                resultCategories.Add(new
                {
                    cat.Id,
                    cat.Name,
                    cat.MonthlyLimit,
                    cat.Icon,
                    Spent = (bankSpent / 100) + manualSpent
                });
            }

            return Ok(new
            {
                totalCapital = bankBalance + manualBalance,
                bankBalance,
                manualBalance,
                // Передаємо список гаманців, щоб фронтенд знав їхні ID
                manualWallets = manualWallets.Select(w => new { w.Id, w.Name, w.Balance }),
                categories = resultCategories
            });
        }

        [HttpPost("manual-transaction")]
        public async Task<IActionResult> AddManualTransaction([FromBody] ManualTransactionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wallet = await _context.ManualWallets
                .FirstOrDefaultAsync(w => w.Id == dto.WalletId && w.UserId == userId);

            if (wallet == null) return BadRequest("Гаманець не знайдено. Перевірте WalletId.");

            // 1. Оновлюємо баланс гаманця
            if (dto.IsIncome) wallet.Balance += dto.Amount;
            else wallet.Balance -= dto.Amount;

            // 2. Створюємо запис в історію
            var transaction = new ManualTransaction
            {
                Amount = dto.IsIncome ? dto.Amount : -dto.Amount,
                Description = dto.Description ?? (dto.IsIncome ? "Прибуток" : "Витрата"),
                Date = DateTime.UtcNow,
                CategoryId = dto.CategoryId,
                WalletId = dto.WalletId,
                UserId = userId
            };

            _context.ManualTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { newBalance = wallet.Balance });
        }

        [HttpPost("set-mcc-mapping")]
        public async Task<IActionResult> SetMccMapping(int mcc, int categoryId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.MccMappings.FirstOrDefaultAsync(m => m.Mcc == mcc && m.UserId == userId);

            if (existing != null) existing.CategoryId = categoryId;
            else _context.MccMappings.Add(new MccMapping { Mcc = mcc, CategoryId = categoryId, UserId = userId });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            long unixStartOfMonth = ((DateTimeOffset)startOfMonth).ToUnixTimeSeconds();

            // 1. Витрати по категоріях (існуюча логіка)
            var result = await GetBudgetSummary() as OkObjectResult;
            var summaryData = result?.Value as dynamic;
            var categories = summaryData?.categories as IEnumerable<object>;

            // 2. Витрати за останні 7 днів (для лінійного графіка)
            var last7Days = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var nextDate = date.AddDays(1);
                long dateUnix = ((DateTimeOffset)date).ToUnixTimeSeconds();
                long nextDateUnix = ((DateTimeOffset)nextDate).ToUnixTimeSeconds();

                var dailySpent = await _context.BankTransactions
                    .Where(t => t.BankAccount.UserId == userId &&
                                t.Amount < 0 &&
                                t.Time >= dateUnix &&
                                t.Time < nextDateUnix)
                    .SumAsync(t => Math.Abs(t.Amount));

                var manualSpent = await _context.ManualTransactions
                    .Where(t => t.UserId == userId &&
                                t.Amount < 0 &&
                                t.Date >= date &&
                                t.Date < nextDate)
                    .SumAsync(t => Math.Abs(t.Amount));

                last7Days.Add(new
                {
                    date = date.ToString("dd.MM"),
                    spent = dailySpent + manualSpent
                });
            }

            // 3. Топ 5 найбільших витрат цього місяця
            var topTransactions = await _context.BankTransactions
                .Where(t => t.BankAccount.UserId == userId &&
                            t.Amount < 0 &&
                            t.Time >= unixStartOfMonth)
                .OrderBy(t => t.Amount)
                .Take(5)
                .Select(t => new
                {
                    description = t.Description,
                    amount = Math.Abs(t.Amount),
                    date = t.Time
                })
                .ToListAsync();

            var topManual = await _context.ManualTransactions
                .Where(t => t.UserId == userId &&
                            t.Amount < 0 &&
                            t.Date >= startOfMonth)
                .OrderBy(t => t.Amount)
                .Take(5)
                .Select(t => new
                {
                    description = t.Description,
                    amount = Math.Abs(t.Amount),
                    date = ((DateTimeOffset)t.Date).ToUnixTimeSeconds()
                })
                .ToListAsync();

            var allTop = topTransactions.Concat(topManual)
                .OrderByDescending(t => t.amount)
                .Take(5)
                .ToList();

            // 4. Загальна статистика
            var totalSpent = categories?.Cast<dynamic>().Sum(c => (decimal)c.Spent) ?? 0;
            var avgDaily = last7Days.Cast<dynamic>().Average(d => (decimal)d.spent);

            return Ok(new
            {
                categories,
                last7Days,
                topTransactions = allTop,
                statistics = new
                {
                    totalSpent,
                    avgDaily,
                    daysInMonth = DateTime.DaysInMonth(now.Year, now.Month),
                    projection = avgDaily * DateTime.DaysInMonth(now.Year, now.Month)
                }
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetFullHistory()
        {
            // 1. Отримуємо ID поточного користувача
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Отримуємо налаштування користувача (прапорець IsBankSyncEnabled)
            // Оскільки ми додали це поле в модель User (IdentityUser), шукаємо його в таблиці Users
            var user = await _context.Users
                .AsNoTracking() // Оптимізація: тільки читання
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isBankEnabled = user?.IsBankSyncEnabled ?? true;

            var combinedHistory = new List<TransactionHistoryDto>();

            // 3. БЛОК БАНКІВСЬКИХ ТРАНЗАКЦІЙ (Тільки якщо синхронізація увімкнена)
            if (isBankEnabled)
            {
                // Отримуємо словник MCC кодів для цього юзера, щоб підставити назви категорій
                var mappings = await _context.MccMappings
                    .AsNoTracking()
                    .Include(m => m.Category)
                    .Where(m => m.UserId == userId)
                    .ToDictionaryAsync(m => m.Mcc, m => m.Category.Name);

                // Беремо останні 30 банківських операцій
                var rawBankTransactions = await _context.BankTransactions
                    .AsNoTracking()
                    .Where(t => t.BankAccount.UserId == userId)
                    .OrderByDescending(t => t.Time)
                    .Take(30)
                    .ToListAsync();

                // Перетворюємо їх у єдиний формат DTO
                var bankMapped = rawBankTransactions.Select(t => new TransactionHistoryDto
                {
                    Id = "B" + t.Id, // Префікс 'B' для унікальності ID на фронтенді
                    Amount = t.Amount, // Сума вже в гривнях (конвертація відбулась при збереженні)
                    Description = t.Description,
                    Time = t.Time, // Вже в форматі Unix Timestamp
                    Source = "Bank",
                    // Шукаємо назву категорії у нашому словнику мапінгів
                    CategoryName = mappings.ContainsKey(t.Mcc) ? mappings[t.Mcc] : "Без категорії",
                    CategoryIcon = "bank"
                });

                combinedHistory.AddRange(bankMapped);
            }

            // 4. БЛОК РУЧНИХ ТРАНЗАКЦІЙ (Додаємо завжди)
            var manualTransactions = await _context.ManualTransactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(30)
                .ToListAsync();

            var manualMapped = manualTransactions.Select(t => new TransactionHistoryDto
            {
                Id = "M" + t.Id, // Префікс 'M' для ручних записів
                Amount = t.Amount,
                Description = t.Description,
                // Конвертуємо DateTime (C#) у Unix Timestamp (seconds) для сортування
                Time = ((DateTimeOffset)t.Date).ToUnixTimeSeconds(),
                Source = "Manual",
                CategoryName = t.Category != null ? t.Category.Name : "Готівка",
                CategoryIcon = "wallet"
            });

            combinedHistory.AddRange(manualMapped);

            // 5. ФІНАЛЬНЕ ОБ'ЄДНАННЯ ТА СОРТУВАННЯ
            // Сортуємо весь змішаний список за часом (найновіші зверху) і беремо топ-40
            var result = combinedHistory
                .OrderByDescending(t => t.Time)
                .Take(40)
                .ToList();

            return Ok(result);
        }

        [HttpPost("accounts/{id}/toggle-inclusion")]
        public async Task<IActionResult> ToggleInclusion(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (account == null) return NotFound("Рахунок не знайдено");

            account.IsIncludedInTotal = !account.IsIncludedInTotal;
            await _context.SaveChangesAsync();
            return Ok(new { isIncluded = account.IsIncludedInTotal });
        }

        [HttpPut("manual-transaction/{id}")]
        public async Task<IActionResult> UpdateManualTransaction(int id, [FromBody] ManualTransactionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await _context.ManualTransactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound("Транзакцію не знайдено");

            var wallet = await _context.ManualWallets.FirstOrDefaultAsync(w => w.Id == transaction.WalletId);
            if (wallet == null) return NotFound("Гаманець не знайдено");
            
            // Відновлюємо баланс гаманця (відміняємо стару транзакцію)
            wallet.Balance -= transaction.Amount;

            // Оновлюємо дані транзакції
            transaction.Amount = dto.IsIncome ? dto.Amount : -dto.Amount;
            transaction.Description = dto.Description ?? transaction.Description;
            transaction.CategoryId = dto.CategoryId ?? transaction.CategoryId;

            // Застосовуємо нову транзакцію до балансу
            wallet.Balance += transaction.Amount;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Транзакцію оновлено", newBalance = wallet.Balance });
        }

        [HttpDelete("manual-transaction/{id}")]
        public async Task<IActionResult> DeleteManualTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await _context.ManualTransactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound("Транзакцію не знайдено");

            var wallet = await _context.ManualWallets.FirstOrDefaultAsync(w => w.Id == transaction.WalletId);
            if (wallet != null)
            {
                // Відновлюємо баланс (відміняємо транзакцію)
                wallet.Balance -= transaction.Amount;
            }

            _context.ManualTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Транзакцію видалено", newBalance = wallet?.Balance });
        }

        [HttpDelete("bank-transaction/{id}")]
        public async Task<IActionResult> DeleteBankTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await _context.BankTransactions
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.BankAccount.UserId == userId);

            if (transaction == null) return NotFound("Транзакцію не знайдено");

            _context.BankTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Транзакцію видалено" });
        }

        [HttpPost("category")]
        public async Task<IActionResult> AddCategory([FromBody] CategoryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var category = new BudgetCategory
            {
                Name = dto.Name,
                MonthlyLimit = dto.MonthlyLimit,
                Icon = dto.Icon ?? "tag.fill",
                UserId = userId!
            };
            
            _context.BudgetCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [HttpPut("category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _context.BudgetCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null) return NotFound("Категорію не знайдено");

            category.Name = dto.Name;
            category.MonthlyLimit = dto.MonthlyLimit;
            category.Icon = dto.Icon ?? category.Icon;

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("category/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _context.BudgetCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null) return NotFound("Категорію не знайдено");

            _context.BudgetCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорію видалено" });
        }
    }
}