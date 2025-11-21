using Amazon.S3;
using Amazon.S3.Model;
using CodeLogic.Abstractions;
using CL.StorageS3.Models;

namespace CL.StorageS3.Services;

/// <summary>
/// Service for S3 storage operations (buckets, objects, uploads, downloads)
/// </summary>
public class S3StorageService
{
    private readonly S3ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private readonly string _connectionId;
    private readonly string? _publicUrl;

    /// <summary>
    /// Initializes a new instance of S3StorageService
    /// </summary>
    /// <param name="connectionManager">Connection manager instance</param>
    /// <param name="connectionId">Connection identifier</param>
    /// <param name="logger">Optional logger instance</param>
    public S3StorageService(
        S3ConnectionManager connectionManager,
        string connectionId = "Default",
        ILogger? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _connectionId = connectionId;
        _logger = logger;

        // Get public URL from configuration if available
        var config = _connectionManager.GetConfiguration(connectionId);
        _publicUrl = config?.PublicUrl;
    }

    #region Bucket Operations

    /// <summary>
    /// Creates a new S3 bucket
    /// </summary>
    /// <param name="bucketName">Name of the bucket to create</param>
    /// <returns>Operation result</returns>
    public async Task<OperationResult<bool>> CreateBucketAsync(string bucketName)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Creating bucket: {bucketName}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            };

            var response = await client.PutBucketAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Bucket created: {bucketName}");

            return OperationResult<bool>.Ok(true, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to create bucket '{bucketName}': {ex.Message}");
            return OperationResult<bool>.Fail($"Failed to create bucket: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes an S3 bucket (must be empty)
    /// </summary>
    /// <param name="bucketName">Name of the bucket to delete</param>
    /// <returns>Operation result</returns>
    public async Task<OperationResult<bool>> DeleteBucketAsync(string bucketName)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Deleting bucket: {bucketName}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new DeleteBucketRequest
            {
                BucketName = bucketName
            };

            var response = await client.DeleteBucketAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Bucket deleted: {bucketName}");

            return OperationResult<bool>.Ok(true, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to delete bucket '{bucketName}': {ex.Message}");
            return OperationResult<bool>.Fail($"Failed to delete bucket: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lists all buckets
    /// </summary>
    /// <returns>Operation result with list of buckets</returns>
    public async Task<OperationResult<List<Models.BucketInfo>>> ListBucketsAsync()
    {
        try
        {
            _logger?.LogInfo("S3StorageService", "Listing all buckets");

            var client = _connectionManager.GetClient(_connectionId);

            var response = await client.ListBucketsAsync();

            var buckets = response.Buckets.Select(Models.BucketInfo.FromS3Bucket).ToList();

            _logger?.LogSuccess("S3StorageService", $"Found {buckets.Count} buckets");

            return OperationResult<List<Models.BucketInfo>>.Ok(buckets, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to list buckets: {ex.Message}");
            return OperationResult<List<Models.BucketInfo>>.Fail($"Failed to list buckets: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a bucket exists
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <returns>True if bucket exists</returns>
    public async Task<bool> BucketExistsAsync(string bucketName)
    {
        try
        {
            var client = _connectionManager.GetClient(_connectionId);
            var response = await client.ListBucketsAsync();
            return response.Buckets.Any(b => b.BucketName.Equals(bucketName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Object Upload Operations

    /// <summary>
    /// Uploads an object to S3 from byte array
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="data">Data to upload</param>
    /// <param name="options">Upload options</param>
    /// <returns>Operation result with object info</returns>
    public async Task<OperationResult<S3ObjectInfo>> PutObjectAsync(
        string bucketName,
        string key,
        byte[] data,
        UploadOptions? options = null)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Uploading object: {bucketName}/{key} ({data.Length} bytes)");

            options ??= UploadOptions.Default();

            // Ensure path exists
            await EnsurePathExistsAsync(bucketName, key);

            var client = _connectionManager.GetClient(_connectionId);

            using var stream = new MemoryStream(data);

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = options.ContentType ?? "application/octet-stream"
            };

            ApplyUploadOptions(request, options);

            var response = await client.PutObjectAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Object uploaded: {bucketName}/{key}");

            var objectInfo = new S3ObjectInfo
            {
                Key = key,
                BucketName = bucketName,
                Size = data.Length,
                ETag = response.ETag,
                PublicUrl = _publicUrl != null ? $"{_publicUrl.TrimEnd('/')}/{bucketName}/{key}" : null
            };

            return OperationResult<S3ObjectInfo>.Ok(objectInfo, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to upload object '{bucketName}/{key}': {ex.Message}");
            return OperationResult<S3ObjectInfo>.Fail($"Failed to upload object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Uploads an object to S3 from stream
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="stream">Stream to upload</param>
    /// <param name="options">Upload options</param>
    /// <returns>Operation result with object info</returns>
    public async Task<OperationResult<S3ObjectInfo>> PutObjectStreamAsync(
        string bucketName,
        string key,
        Stream stream,
        UploadOptions? options = null)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Uploading object from stream: {bucketName}/{key}");

            options ??= UploadOptions.Default();

            // Ensure path exists
            await EnsurePathExistsAsync(bucketName, key);

            var client = _connectionManager.GetClient(_connectionId);

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = options.ContentType ?? "application/octet-stream"
            };

            ApplyUploadOptions(request, options);

            var response = await client.PutObjectAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Object uploaded from stream: {bucketName}/{key}");

            var objectInfo = new S3ObjectInfo
            {
                Key = key,
                BucketName = bucketName,
                Size = stream.Length,
                ETag = response.ETag,
                PublicUrl = _publicUrl != null ? $"{_publicUrl.TrimEnd('/')}/{bucketName}/{key}" : null
            };

            return OperationResult<S3ObjectInfo>.Ok(objectInfo, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to upload object from stream '{bucketName}/{key}': {ex.Message}");
            return OperationResult<S3ObjectInfo>.Fail($"Failed to upload object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Uploads a file to S3
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="filePath">Local file path</param>
    /// <param name="options">Upload options</param>
    /// <returns>Operation result with object info</returns>
    public async Task<OperationResult<S3ObjectInfo>> PutObjectFileAsync(
        string bucketName,
        string key,
        string filePath,
        UploadOptions? options = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return OperationResult<S3ObjectInfo>.Fail($"File not found: {filePath}");
            }

            _logger?.LogInfo("S3StorageService", $"Uploading file: {filePath} to {bucketName}/{key}");

            using var fileStream = File.OpenRead(filePath);
            return await PutObjectStreamAsync(bucketName, key, fileStream, options);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to upload file '{filePath}': {ex.Message}");
            return OperationResult<S3ObjectInfo>.Fail($"Failed to upload file: {ex.Message}", ex);
        }
    }

    #endregion

    #region Object Download Operations

    /// <summary>
    /// Downloads an object from S3 as a stream
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="options">Download options</param>
    /// <returns>Operation result with stream</returns>
    public async Task<OperationResult<Stream>> GetObjectAsync(
        string bucketName,
        string key,
        DownloadOptions? options = null)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Downloading object: {bucketName}/{key}");

            options ??= DownloadOptions.Default();

            var client = _connectionManager.GetClient(_connectionId);

            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            if (options.RangeStart.HasValue || options.RangeEnd.HasValue)
            {
                var start = options.RangeStart ?? 0;
                var end = options.RangeEnd.HasValue ? options.RangeEnd.Value.ToString() : "";
                request.ByteRange = new ByteRange(start, options.RangeEnd ?? long.MaxValue);
            }

            if (!string.IsNullOrWhiteSpace(options.VersionId))
            {
                request.VersionId = options.VersionId;
            }

            var response = await client.GetObjectAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Object downloaded: {bucketName}/{key}");

            return OperationResult<Stream>.Ok(response.ResponseStream, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to download object '{bucketName}/{key}': {ex.Message}");
            return OperationResult<Stream>.Fail($"Failed to download object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads an object from S3 as byte array
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="options">Download options</param>
    /// <returns>Operation result with byte array</returns>
    public async Task<OperationResult<byte[]>> GetObjectBytesAsync(
        string bucketName,
        string key,
        DownloadOptions? options = null)
    {
        try
        {
            var result = await GetObjectAsync(bucketName, key, options);

            if (!result.Success || result.Data == null)
            {
                return OperationResult<byte[]>.Fail(result.Error ?? "Failed to download object");
            }

            using var stream = result.Data;
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            return OperationResult<byte[]>.Ok(memoryStream.ToArray(), result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to download object bytes '{bucketName}/{key}': {ex.Message}");
            return OperationResult<byte[]>.Fail($"Failed to download object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads an object from S3 to a local file
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <param name="filePath">Local file path to save to</param>
    /// <param name="options">Download options</param>
    /// <returns>Operation result</returns>
    public async Task<OperationResult<bool>> DownloadObjectFileAsync(
        string bucketName,
        string key,
        string filePath,
        DownloadOptions? options = null)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Downloading object to file: {bucketName}/{key} -> {filePath}");

            var result = await GetObjectAsync(bucketName, key, options);

            if (!result.Success || result.Data == null)
            {
                return OperationResult<bool>.Fail(result.Error ?? "Failed to download object");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = result.Data;
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            _logger?.LogSuccess("S3StorageService", $"Object downloaded to file: {filePath}");

            return OperationResult<bool>.Ok(true, result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to download object to file '{filePath}': {ex.Message}");
            return OperationResult<bool>.Fail($"Failed to download object: {ex.Message}", ex);
        }
    }

    #endregion

    #region Object Management Operations

    /// <summary>
    /// Deletes an object from S3
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <returns>Operation result</returns>
    public async Task<OperationResult<bool>> DeleteObjectAsync(string bucketName, string key)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Deleting object: {bucketName}/{key}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            var response = await client.DeleteObjectAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Object deleted: {bucketName}/{key}");

            return OperationResult<bool>.Ok(true, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService", $"Failed to delete object '{bucketName}/{key}': {ex.Message}");
            return OperationResult<bool>.Fail($"Failed to delete object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Copies an object within S3
    /// </summary>
    /// <param name="sourceBucket">Source bucket name</param>
    /// <param name="sourceKey">Source object key</param>
    /// <param name="destinationBucket">Destination bucket name</param>
    /// <param name="destinationKey">Destination object key</param>
    /// <returns>Operation result</returns>
    public async Task<OperationResult<bool>> CopyObjectAsync(
        string sourceBucket,
        string sourceKey,
        string destinationBucket,
        string destinationKey)
    {
        try
        {
            _logger?.LogInfo("S3StorageService",
                $"Copying object: {sourceBucket}/{sourceKey} -> {destinationBucket}/{destinationKey}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = sourceKey,
                DestinationBucket = destinationBucket,
                DestinationKey = destinationKey
            };

            var response = await client.CopyObjectAsync(request);

            _logger?.LogSuccess("S3StorageService", $"Object copied successfully");

            return OperationResult<bool>.Ok(true, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService",
                $"Failed to copy object '{sourceBucket}/{sourceKey}': {ex.Message}");
            return OperationResult<bool>.Fail($"Failed to copy object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets object metadata without downloading the object
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <returns>Operation result with object info</returns>
    public async Task<OperationResult<S3ObjectInfo>> GetObjectMetadataAsync(string bucketName, string key)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Getting object metadata: {bucketName}/{key}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key
            };

            var response = await client.GetObjectMetadataAsync(request);

            var objectInfo = S3ObjectInfo.FromMetadata(response, key, bucketName, _publicUrl);

            _logger?.LogSuccess("S3StorageService", $"Object metadata retrieved: {bucketName}/{key}");

            return OperationResult<S3ObjectInfo>.Ok(objectInfo, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService",
                $"Failed to get object metadata '{bucketName}/{key}': {ex.Message}");
            return OperationResult<S3ObjectInfo>.Fail($"Failed to get metadata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if an object exists
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="key">Object key (path)</param>
    /// <returns>True if object exists</returns>
    public async Task<bool> ObjectExistsAsync(string bucketName, string key)
    {
        try
        {
            var result = await GetObjectMetadataAsync(bucketName, key);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region List Operations

    /// <summary>
    /// Lists objects in a bucket with optional prefix filtering
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="prefix">Optional prefix filter</param>
    /// <param name="maxKeys">Maximum number of keys to return</param>
    /// <param name="continuationToken">Continuation token for pagination</param>
    /// <returns>Operation result with list of objects</returns>
    public async Task<OperationResult<ListObjectsResult>> ListObjectsAsync(
        string bucketName,
        string? prefix = null,
        int maxKeys = 1000,
        string? continuationToken = null)
    {
        try
        {
            _logger?.LogInfo("S3StorageService", $"Listing objects in bucket: {bucketName}");

            var client = _connectionManager.GetClient(_connectionId);

            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                MaxKeys = maxKeys
            };

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                request.Prefix = prefix;
            }

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                request.ContinuationToken = continuationToken;
            }

            var response = await client.ListObjectsV2Async(request);

            var result = new ListObjectsResult
            {
                Objects = response.S3Objects.Select(obj =>
                    S3ObjectInfo.FromS3Object(obj, bucketName, _publicUrl)).ToList(),
                NextContinuationToken = response.NextContinuationToken,
                IsTruncated = response.IsTruncated ?? false,
                CommonPrefixes = response.CommonPrefixes
            };

            _logger?.LogSuccess("S3StorageService",
                $"Found {result.Count} objects in bucket: {bucketName}");

            return OperationResult<ListObjectsResult>.Ok(result, (int)response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3StorageService",
                $"Failed to list objects in bucket '{bucketName}': {ex.Message}");
            return OperationResult<ListObjectsResult>.Fail($"Failed to list objects: {ex.Message}", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensures the folder path exists in S3 by creating placeholder objects
    /// </summary>
    private async Task EnsurePathExistsAsync(string bucketName, string key)
    {
        try
        {
            var client = _connectionManager.GetClient(_connectionId);

            // Extract folder path from key
            var lastSlashIndex = key.LastIndexOf('/');
            if (lastSlashIndex <= 0)
            {
                return; // No folder path to create
            }

            var folderPath = key.Substring(0, lastSlashIndex + 1);

            // Check if folder marker exists
            if (await ObjectExistsAsync(bucketName, folderPath))
            {
                return;
            }

            // Create folder marker (empty object with trailing /)
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = folderPath,
                ContentBody = string.Empty
            };

            await client.PutObjectAsync(request);

            _logger?.LogInfo("S3StorageService", $"Created folder path: {bucketName}/{folderPath}");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("S3StorageService",
                $"Failed to create folder path for '{key}': {ex.Message}");
            // Non-critical error, continue with upload
        }
    }

    /// <summary>
    /// Applies upload options to a PutObjectRequest
    /// </summary>
    private void ApplyUploadOptions(PutObjectRequest request, UploadOptions options)
    {
        if (options.Metadata != null)
        {
            foreach (var kvp in options.Metadata)
            {
                request.Metadata[kvp.Key] = kvp.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.StorageClass))
        {
            request.StorageClass = S3StorageClass.FindValue(options.StorageClass);
        }

        if (!string.IsNullOrWhiteSpace(options.ServerSideEncryption))
        {
            request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.FindValue(options.ServerSideEncryption);
        }

        if (!string.IsNullOrWhiteSpace(options.CacheControl))
        {
            request.Headers.CacheControl = options.CacheControl;
        }

        if (!string.IsNullOrWhiteSpace(options.ContentDisposition))
        {
            request.Headers.ContentDisposition = options.ContentDisposition;
        }

        if (options.MakePublic)
        {
            request.CannedACL = S3CannedACL.PublicRead;
        }
    }

    #endregion
}
