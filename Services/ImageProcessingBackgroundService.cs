using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ABCRetailers.Services
{
    public class ImageProcessingBackgroundService : BackgroundService
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ImageProcessingBackgroundService> _logger;

        public ImageProcessingBackgroundService(
            IAzureStorageService storageService,
            ILogger<ImageProcessingBackgroundService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image Processing Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _storageService.ReceiveMessageAsync("image-processing");

                    if (message != null)
                    {
                        await ProcessImageMessageAsync(message);
                    }
                    else
                    {
                        await Task.Delay(10000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in image processing service");
                    await Task.Delay(15000, stoppingToken);
                }
            }

            _logger.LogInformation("Image Processing Background Service is stopping.");
        }

        private async Task ProcessImageMessageAsync(string message)
        {
            try
            {
                var imageMessage = JsonSerializer.Deserialize<ImageProcessingMessage>(message);

                if (imageMessage != null)
                {
                    _logger.LogInformation("Processing image for product: {ProductName}, Action: {Action}",
                        imageMessage.ProductName, imageMessage.Action);

                    // Simulate image processing tasks:
                    // - Generate thumbnails
                    // - Optimize image size
                    // - Extract metadata
                    // - Update database records

                    await Task.Delay(2000); // Simulate processing time

                    _logger.LogInformation("Completed image processing for: {ProductName}", imageMessage.ProductName);

                    // Log to Azure Tables
                    await _storageService.AddEntityAsync(new ImageProcessLog
                    {
                        PartitionKey = "ImageProcess",
                        RowKey = Guid.NewGuid().ToString(),
                        ProductId = imageMessage.ProductId,
                        ProductName = imageMessage.ProductName,
                        Action = imageMessage.Action,
                        ProcessTime = DateTime.UtcNow,
                        Status = "Completed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image message: {Message}", message);
            }
        }
    }

    public class ImageProcessingMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime ProcessTime { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class ImageProcessLog : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime ProcessTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}