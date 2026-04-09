
using FinGrid.Models;

namespace FinGrid.Service
{
    public class MonobankService
    {
        private readonly HttpClient _httpClient;

        public MonobankService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MonoClientInfo> GetClientInfoAsync(string token)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Token", token);

            var response = await _httpClient.GetAsync("https://api.monobank.ua/personal/client-info");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MonoClientInfo>();
            }

            return null;
        }
        public async Task<List<MonoStatementItem>> GetStatementsAsync(string token, string accountId)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Token", token);

            // Отримуємо дані за останні 31 день (Unix час)
            long from = DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeSeconds();
            var response = await _httpClient.GetAsync($"https://api.monobank.ua/personal/statement/{accountId}/{from}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<MonoStatementItem>>();
            }
            return null;
        }
    }
}
