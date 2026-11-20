namespace WACAMS.Infrastructure.Storage;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the URL
    /// </summary>
    Task<string> UploadFileAsync(byte[] fileContent, string fileName, string contentType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uploads a file to a specific container/folder
    /// </summary>
    Task<string> UploadFileAsync(byte[] fileContent, string containerName, string fileName, string contentType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads a file from storage
    /// </summary>
    Task<byte[]> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a SAS token URL for temporary access
    /// </summary>
    Task<string> GetSasUrlAsync(string fileUrl, TimeSpan expiry, CancellationToken cancellationToken = default);
}
