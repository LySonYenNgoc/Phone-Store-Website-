using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace PhoneStore.Services;

public interface IProductImageService
{
    Task<byte[]?> ProcessImageAsync(IFormFile image, int maxWidth = 1920, int maxHeight = 1080);
}

public class ProductImageService : IProductImageService
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly ILogger<ProductImageService> _logger;

    public ProductImageService(ILogger<ProductImageService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]?> ProcessImageAsync(IFormFile image, int maxWidth = 1920, int maxHeight = 1080)
    {
        if (image == null || image.Length == 0) return null;

        try
        {
            // Check file size
            if (image.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException($"File size exceeds {MaxFileSizeBytes / 1024 / 1024}MB limit");
            }

            // Check file extension (Optional: can be skipped for 'allow all')
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            // Allow all for now as requested
            /*
            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Allowed types: " + string.Join(", ", AllowedExtensions));
            }
            */

            // Check if it's really an image (Optional)
            /*
            if (!image.ContentType.StartsWith("image/"))
            {
                throw new ArgumentException("File is not a valid image");
            }
            */

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            byte[] originalBytes = memoryStream.ToArray();

            try {
                memoryStream.Position = 0;
                using var img = await Image.LoadAsync(memoryStream);

                // Resize if needed
                if (img.Width > maxWidth || img.Height > maxHeight)
                {
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new SixLabors.ImageSharp.Size(maxWidth, maxHeight)
                    }));
                }

                // Compress and convert to JPEG
                using var outputStream = new MemoryStream();
                await img.SaveAsync(outputStream, new JpegEncoder
                {
                    Quality = 80 // Balanced quality
                });

                _logger.LogInformation(
                    "Processed image {FileName}: Original size {OriginalSize}KB, Final size {FinalSize}KB", 
                    image.FileName,
                    image.Length / 1024,
                    outputStream.Length / 1024);

                return outputStream.ToArray();
            }
            catch (Exception) {
                // If it's not a valid image or fails processing, return original bytes
                _logger.LogInformation("File {FileName} is not a valid image or failed processing, returning original bytes", image.FileName);
                return originalBytes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image {FileName}", image.FileName);
            throw;
        }
    }
}
