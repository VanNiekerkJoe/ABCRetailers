using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public class OrderQueueBackgroundService : BackgroundService
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrderQueueBackgroundService> _logger;

        public OrderQueueBackgroundService(
            IAzureStorageService storageService,
            ILogger<OrderQueueBackgroundService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Queue Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check for new messages in the queue
                    var message = await _storageService.ReceiveMessageAsync("order-notifications");

                    if (message != null)
                    {
                        await ProcessOrderMessageAsync(message);
                    }
                    else
                    {
                        // No messages, wait before checking again
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order queue");
                    await Task.Delay(10000, stoppingToken);
                }
            }

            _logger.LogInformation("Order Queue Background Service is stopping.");
        }

        private async Task ProcessOrderMessageAsync(string message)
        {
            try
            {
                var orderMessage = JsonSerializer.Deserialize<OrderNotificationMessage>(message);

                if (orderMessage != null)
                {
                    _logger.LogInformation("Processing order: {OrderId}, Status: {Status}",
                        orderMessage.OrderId, orderMessage.Status);

                    // Process based on order status
                    switch (orderMessage.Status)
                    {
                        case "Submitted":
                            await ProcessSubmittedOrder(orderMessage);
                            break;
                        case "Processing":
                            await ProcessProcessingOrder(orderMessage);
                            break;
                        case "Completed":
                            await ProcessCompletedOrder(orderMessage);
                            break;
                        case "Cancelled":
                            await ProcessCancelledOrder(orderMessage);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order message: {Message}", message);
            }
        }

        private async Task ProcessSubmittedOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} submitted by {CustomerName}", order.OrderId, order.CustomerName);
            await Task.Delay(1000);
        }

        private async Task ProcessProcessingOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} is now processing", order.OrderId);
            await Task.Delay(1000);
        }

        private async Task ProcessCompletedOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} completed successfully", order.OrderId);
            await Task.Delay(1000);
        }

        private async Task ProcessCancelledOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} was cancelled", order.OrderId);
            await Task.Delay(1000);
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
}