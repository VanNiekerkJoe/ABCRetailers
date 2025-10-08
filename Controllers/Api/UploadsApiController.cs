using ABCRetailers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadsController> _logger;

        public UploadsController(IAzureStorageService storageService, ILogger<UploadsController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [HttpPost("proof-of-payment")]
        public async Task<IActionResult> UploadProofOfPayment(IFormFile file, string orderId, string customerName)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Write to Blob Storage
                var blobFileName = await _storageService.UploadFileAsync(file, "payment-proofs");

                // Write to Azure Files
                var fileShareName = await _storageService.UploadToFileShareAsync(file, "contracts", "payments");

                return Ok(new
                {
                    Message = "Proof of payment uploaded successfully",
                    BlobFileName = blobFileName,
                    FileShareName = fileShareName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading proof of payment");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}