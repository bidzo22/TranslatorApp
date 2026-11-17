using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace TranslatorApp.Services;

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = "";
    public string Container { get; set; } = "images";
}

public class BlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<BlobStorageOptions> options, ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ConnectionString);

    private BlobContainerClient GetContainer() =>
        new BlobContainerClient(_options.ConnectionString, _options.Container);

    public async Task<string> UploadAsync(IFormFile file)
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{ext}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        _logger.LogInformation("Зображення збережено в Blob Storage: {Blob}", blobName);
        return blobName;
    }

    public Uri GetReadSasUri(string blobName, TimeSpan lifetime)
    {
        var blob = GetContainer().GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "Неможливо створити SAS: у Connection String має бути AccountKey.");

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime));
    }
}