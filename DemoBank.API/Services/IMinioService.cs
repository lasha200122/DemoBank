namespace DemoBank.API.Services;

public interface IMinioService
{
    Task<string> UploadFileAsync(IFormFile file, string bucketName, string objectName = null);
    Task<(byte[] fileContent, string contentType)> DownloadFileAsync(string bucketName, string objectName);
    Task DeleteFileAsync(string bucketName, string objectName);
    Task<bool> FileExistsAsync(string bucketName, string objectName);
    Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600);
    Task EnsureBucketExistsAsync(string bucketName);
}