using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using WACAMS.Core.Interfaces;

namespace Infrastructure.Services.Storage;

/// <summary>
/// Azure Blob Storage implementation of file storage service
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _defaultContainer;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        string defaultContainer,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _defaultContainer = defaultContainer;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(
        byte[] fileContent, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        return await UploadFileAsync(fileContent, _defaultContainer, fileName, contentType, cancellationToken);
    }

    public async Task<string> UploadFileAsync(
        byte[] fileContent, 
        string containerName, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            
            // Generate unique blob name with date-based folder structure
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
            var uniqueFileName = $"{dateFolder}/{Guid.NewGuid()}_{fileName}";
            
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            // Upload the file
            using var stream = new MemoryStream(fileContent);
            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("File uploaded successfully to Azure Blob Storage: {FileName}", uniqueFileName);
            
            // Return the blob URL
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(fileUrl));
            var response = await blobClient.DownloadAsync(cancellationToken);
            
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream, cancellationToken);
            
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Azure Blob Storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(fileUrl));
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("File deleted from Azure Blob Storage: {FileUrl}", fileUrl);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Azure Blob Storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(fileUrl));
            var exists = await blobClient.ExistsAsync(cancellationToken);
            return exists.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if file exists in Azure Blob Storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<string> GetSasUrlAsync(string fileUrl, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(fileUrl));
            
            // Generate SAS token with read permissions
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
            };
            
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            
            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            
            return sasToken.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URL for file: {FileUrl}", fileUrl);
            throw;
        }
    }
}
