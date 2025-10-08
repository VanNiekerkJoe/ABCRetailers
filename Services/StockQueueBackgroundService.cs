using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public class StockQueueBackgroundService : BackgroundService
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<StockQueueBackgroundService> _logger;

        public StockQueueBackgroundService(
            IAzureStorageService storageService,
            ILogger<StockQueueBackgroundService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Queue Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _storageService.ReceiveMessageAsync("stock-updates");

                    if (message != null)
                    {
                        await ProcessStockMessageAsync(message);
                    }
                    else
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stock queue");
                    await Task.Delay(10000, stoppingToken);
                }
            }

            _logger.LogInformation("Stock Queue Background Service is stopping.");
        }

        private async Task ProcessStockMessageAsync(string message)
        {
            try
            {
                var stockMessage = JsonSerializer.Deserialize<StockUpdateMessage>(message);

                if (stockMessage != null)
                {
                    _logger.LogInformation("Stock update - Product: {ProductName}, New stock: {NewStock}",
                        stockMessage.ProductName, stockMessage.NewStock);

                    // Check for low stock alerts
                    if (stockMessage.NewStock < 10)
                    {
                        _logger.LogWarning("LOW STOCK ALERT: {ProductName} has only {StockLevel} units remaining",
                            stockMessage.ProductName, stockMessage.NewStock);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock message: {Message}", message);
            }
        }
    }

    public class StockUpdateMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdateDate { get; set; }
    }
}