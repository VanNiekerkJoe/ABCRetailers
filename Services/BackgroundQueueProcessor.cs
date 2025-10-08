using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Services
{
    public class BackgroundQueueProcessor
    {
        private readonly ILogger<BackgroundQueueProcessor> _logger;
        private readonly IAzureStorageService _storageService;

        public BackgroundQueueProcessor(ILogger<BackgroundQueueProcessor> logger, IAzureStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        [FunctionName("OrderNotificationsProcessor")]
        public void ProcessOrderNotifications(
            [QueueTrigger("order-notifications")] string queueItem)
        {
            _logger.LogInformation("Processing order notification: {QueueItem}", queueItem);

            try
            {
                var orderMessage = JsonSerializer.Deserialize<OrderNotificationMessage>(queueItem);

                if (orderMessage != null)
                {
                    _logger.LogInformation("Order {OrderId} processed with status: {Status}",
                        orderMessage.OrderId, orderMessage.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order notification");
            }
        }

        [FunctionName("StockUpdatesProcessor")]
        public void ProcessStockUpdates(
            [QueueTrigger("stock-updates")] string queueItem)
        {
            _logger.LogInformation("Processing stock update: {QueueItem}", queueItem);

            try
            {
                var stockMessage = JsonSerializer.Deserialize<StockUpdateMessage>(queueItem);

                if (stockMessage != null)
                {
                    _logger.LogInformation("Stock updated for {ProductName}: {NewStock} units",
                        stockMessage.ProductName, stockMessage.NewStock);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock update");
            }
        }

        public class OrderNotificationMessage
        {
            public string OrderId { get; set; } = string.Empty;
            public string CustomerId { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal TotalPrice { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = string.Empty;
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
}