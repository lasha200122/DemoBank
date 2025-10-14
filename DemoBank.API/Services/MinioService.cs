using Minio;
using Minio.DataModel.Args;

namespace DemoBank.API.Services;

public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioService> _logger;

    public MinioService(IConfiguration configuration, ILogger<MinioService> logger)
    {
        _logger = logger;

        var endpoint = configuration["Minio:Endpoint"];
        var accessKey = configuration["Minio:AccessKey"];
        var secretKey = configuration["Minio:SecretKey"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("MinIO configuration is incomplete");
        }

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint.Replace("http://", "").Replace("https://", ""))
            .WithCredentials(accessKey, secretKey)
            .Build();

        _logger.LogInformation("MinIO client initialized with endpoint: {Endpoint}", endpoint);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string bucketName, string objectName = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            // Ensure bucket exists
            await EnsureBucketExistsAsync(bucketName);

            // Generate object name if not provided
            objectName ??= $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            // Upload file
            using var stream = file.OpenReadStream();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("File uploaded successfully: {ObjectName} to bucket: {BucketName}",
                objectName, bucketName);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to MinIO");
            throw;
        }
    }

    public async Task<(byte[] fileContent, string contentType)> DownloadFileAsync(string bucketName, string objectName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            string contentType = "application/octet-stream";

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream) =>
                {
                    await stream.CopyToAsync(memoryStream);
                });

            var stat = await _minioClient.GetObjectAsync(getObjectArgs);
            contentType = stat.ContentType ?? contentType;

            _logger.LogInformation("File downloaded successfully: {ObjectName} from bucket: {BucketName}",
                objectName, bucketName);

            return (memoryStream.ToArray(), contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from MinIO");
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);

            _logger.LogInformation("File deleted successfully: {ObjectName} from bucket: {BucketName}",
                objectName, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from MinIO");
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectName)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogInformation("Presigned URL generated for: {ObjectName}", objectName);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            throw;
        }
    }

    public async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs);

                _logger.LogInformation("Bucket created: {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists");
            throw;
        }
    }
}