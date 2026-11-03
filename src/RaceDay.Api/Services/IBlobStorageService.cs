using System.IO;
using System.Threading.Tasks;

namespace RaceDay.Api.Services;

// Contract for image storage. The controllers depend on this interface rather
// than the concrete class, so swapping Azurite for real Azure later means
// changing one line in Program.cs and nothing else.
public interface IBlobStorageService
{
    // Uploads a file and returns the public URL it can be reached on.
    Task<string> UploadAsync(Stream content, string fileName, string contentType);

    // Deletes a blob given the URL that UploadAsync handed back.
    Task DeleteAsync(string blobUrl);
}