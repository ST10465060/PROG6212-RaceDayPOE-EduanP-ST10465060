using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RaceDay.Api.Services;

// Talks to Azurite in development and would talk to real Azure Blob Storage
// in production. The only difference between the two is the connection string.
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        var containerName = configuration["BlobSettings:ContainerName"];

        // Azurite lags behind the SDK's default REST API version, so we pin an
        // older service version that the emulator definitely understands.
        // Real Azure supports every version, so this is safe either way.
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);

        _container = new BlobContainerClient(connectionString, containerName, options);

        // Create the container the first time the app runs. PublicAccessType.Blob
        // lets a browser load the image directly from the URL, which is what the
        // MVC views need for an ordinary img tag to work.
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        // A GUID name avoids two users overwriting each other by uploading
        // files that happen to share a name, such as banner.jpg.
        var extension = Path.GetExtension(fileName);
        var blobName = $"{Guid.NewGuid()}{extension}";

        var blob = _container.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            // Without this the browser downloads the file instead of showing it.
            ContentType = contentType
        };

        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = headers });

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            return;
        }

        // The blob name is simply the last segment of the stored URL.
        var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
        var blob = _container.GetBlobClient(blobName);

        await blob.DeleteIfExistsAsync();
    }
}