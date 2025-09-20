using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.Infrastructure.Services;

/// <summary>
/// Cloud Blob Storage implementation. Falls back to a local mock if no connection string is provided.
/// </summary>
public class BlobStorageService(IConfiguration config) : IStorageService
{
    private readonly string? _connectionString = config.GetConnectionString("AzureBlob");
    private readonly bool _useLocalMock = string.IsNullOrWhiteSpace(config.GetConnectionString("AzureBlob"));
    private readonly string _localMockDir = Path.Combine(Path.GetTempPath(), "NexaFlowMockBlob");

    public async Task<string> UploadFileAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (_useLocalMock)
        {
            var dir = Path.Combine(_localMockDir, containerName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            using var fs = File.Create(path);
            await content.CopyToAsync(fs, cancellationToken);
            return $"/api/mock-blob/{containerName}/{fileName}"; // Mock URL
        }

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = containerClient.GetBlobClient(fileName);
        
        content.Position = 0;
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteFileAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        if (_useLocalMock)
        {
            var path = Path.Combine(_localMockDir, containerName, fileName);
            if (File.Exists(path)) File.Delete(path);
            return;
        }

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Task<string> GetPresignedUrlAsync(string containerName, string fileName, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (_useLocalMock)
        {
            return Task.FromResult($"/api/mock-blob/{containerName}/{fileName}");
        }

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        if (!blobClient.CanGenerateSasUri) return Task.FromResult(blobClient.Uri.ToString());

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = fileName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return Task.FromResult(blobClient.GenerateSasUri(sasBuilder).ToString());
    }
}
