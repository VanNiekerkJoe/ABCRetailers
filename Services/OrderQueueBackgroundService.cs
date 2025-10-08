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
                    _logger.LogInformation("Processing order: {OrderId}, Status: {Status}, Customer: {CustomerName}",
                        orderMessage.OrderId, orderMessage.Status, orderMessage.CustomerName);

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

                    // Log to Azure Tables for audit trail
                    await _storageService.AddEntityAsync(new OrderProcessLog
                    {
                        PartitionKey = "OrderProcess",
                        RowKey = Guid.NewGuid().ToString(),
                        OrderId = orderMessage.OrderId,
                        ProcessTime = DateTime.UtcNow,
                        Action = $"Processed {orderMessage.Status}",
                        Message = $"Order {orderMessage.OrderId} for {orderMessage.CustomerName}"
                    });
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

            // Here you can add business logic like:
            // - Send confirmation email
            // - Update CRM system
            // - Validate payment
            await Task.Delay(100); // Simulate work
        }

        private async Task ProcessProcessingOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} is now processing", order.OrderId);

            // Business logic for processing:
            // - Update inventory systems
            // - Prepare shipment
            // - Notify warehouse
            await Task.Delay(100);
        }

        private async Task ProcessCompletedOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} completed successfully", order.OrderId);

            // Business logic for completion:
            // - Send delivery confirmation
            // - Update analytics
            // - Request customer review
            await Task.Delay(100);
        }

        private async Task ProcessCancelledOrder(OrderNotificationMessage order)
        {
            _logger.LogInformation("Order {OrderId} was cancelled", order.OrderId);

            // Business logic for cancellation:
            // - Process refunds
            // - Restock inventory
            // - Notify customer service
            await Task.Delay(100);
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

    public class OrderProcessLog : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public DateTime ProcessTime { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}