namespace NexaFlow.Application.Common.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to the blob storage and returns the URL.
    /// </summary>
    Task<string> UploadFileAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the blob storage.
    /// </summary>
    Task DeleteFileAsync(string containerName, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a short-lived presigned URL (SAS token) for secure downloads.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string containerName, string fileName, TimeSpan expiry, CancellationToken cancellationToken = default);
}
