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
                    _logger.LogInformation("Stock update - Product: {ProductName}, Previous: {PreviousStock}, Current: {NewStock}",
                        stockMessage.ProductName, stockMessage.PreviousStock, stockMessage.NewStock);

                    // Check for low stock alerts
                    if (stockMessage.NewStock < 10)
                    {
                        _logger.LogWarning("LOW STOCK ALERT: {ProductName} has only {StockLevel} units remaining",
                            stockMessage.ProductName, stockMessage.NewStock);

                        // You can add alert logic here:
                        // - Send email to procurement team
                        // - Create automatic reorder request
                        // - Update dashboard alerts
                    }

                    // Check for stock increases (restocks)
                    if (stockMessage.NewStock > stockMessage.PreviousStock)
                    {
                        _logger.LogInformation("Stock increased for {ProductName} by {Increase} units",
                            stockMessage.ProductName, stockMessage.NewStock - stockMessage.PreviousStock);
                    }

                    // Log to Azure Tables for audit trail
                    await _storageService.AddEntityAsync(new StockAuditLog
                    {
                        PartitionKey = "StockAudit",
                        RowKey = Guid.NewGuid().ToString(),
                        ProductId = stockMessage.ProductId,
                        ProductName = stockMessage.ProductName,
                        PreviousStock = stockMessage.PreviousStock,
                        NewStock = stockMessage.NewStock,
                        UpdatedBy = stockMessage.UpdatedBy,
                        UpdateDate = stockMessage.UpdateDate,
                        AuditTime = DateTime.UtcNow
                    });
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

    public class StockAuditLog : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdateDate { get; set; }
        public DateTime AuditTime { get; set; }
    }
}