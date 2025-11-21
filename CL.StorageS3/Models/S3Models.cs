using Amazon.S3.Model;

namespace CL.StorageS3.Models;

/// <summary>
/// Generic operation result for S3 operations
/// </summary>
public class OperationResult<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The result data (if successful)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Exception details (if available)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// HTTP status code from S3 operation
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static OperationResult<T> Ok(T data, int statusCode = 200)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static OperationResult<T> Fail(string error, Exception? exception = null, int statusCode = 500)
    {
        return new OperationResult<T>
        {
            Success = false,
            Error = error,
            Exception = exception,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Information about an S3 object
/// </summary>
public class S3ObjectInfo
{
    /// <summary>
    /// Object key (path in bucket)
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Bucket name
    /// </summary>
    public string BucketName { get; set; } = "";

    /// <summary>
    /// Object size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// ETag (entity tag) for version identification
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Storage class (STANDARD, GLACIER, etc.)
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// Object owner
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Public URL for accessing the object
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// Creates S3ObjectInfo from S3Object
    /// </summary>
    public static S3ObjectInfo FromS3Object(S3Object s3Object, string bucketName, string? publicUrl = null)
    {
        var info = new S3ObjectInfo
        {
            Key = s3Object.Key,
            BucketName = bucketName,
            Size = s3Object.Size ?? 0,
            LastModified = s3Object.LastModified ?? DateTime.MinValue,
            ETag = s3Object.ETag,
            StorageClass = s3Object.StorageClass,
            Owner = s3Object.Owner?.DisplayName
        };

        if (!string.IsNullOrWhiteSpace(publicUrl))
        {
            info.PublicUrl = $"{publicUrl.TrimEnd('/')}/{bucketName}/{s3Object.Key}";
        }

        return info;
    }

    /// <summary>
    /// Creates S3ObjectInfo from GetObjectMetadataResponse
    /// </summary>
    public static S3ObjectInfo FromMetadata(GetObjectMetadataResponse metadata, string key, string bucketName, string? publicUrl = null)
    {
        var info = new S3ObjectInfo
        {
            Key = key,
            BucketName = bucketName,
            Size = metadata.ContentLength,
            LastModified = metadata.LastModified ?? DateTime.MinValue,
            ETag = metadata.ETag,
            ContentType = metadata.Headers.ContentType,
            Metadata = metadata.Metadata.Keys.Cast<string>()
                .ToDictionary(k => k, k => metadata.Metadata[k])
        };

        if (!string.IsNullOrWhiteSpace(publicUrl))
        {
            info.PublicUrl = $"{publicUrl.TrimEnd('/')}/{bucketName}/{key}";
        }

        return info;
    }
}

/// <summary>
/// Information about an S3 bucket
/// </summary>
public class BucketInfo
{
    /// <summary>
    /// Bucket name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Bucket region
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Creates BucketInfo from S3Bucket
    /// </summary>
    public static BucketInfo FromS3Bucket(S3Bucket bucket)
    {
        return new BucketInfo
        {
            Name = bucket.BucketName,
            CreationDate = bucket.CreationDate ?? DateTime.MinValue
        };
    }
}

/// <summary>
/// Result of a list objects operation with pagination support
/// </summary>
public class ListObjectsResult
{
    /// <summary>
    /// List of objects
    /// </summary>
    public List<S3ObjectInfo> Objects { get; set; } = new();

    /// <summary>
    /// Continuation token for next page
    /// </summary>
    public string? NextContinuationToken { get; set; }

    /// <summary>
    /// Indicates if there are more results
    /// </summary>
    public bool IsTruncated { get; set; }

    /// <summary>
    /// Common prefixes (folders)
    /// </summary>
    public List<string> CommonPrefixes { get; set; } = new();

    /// <summary>
    /// Total count of objects returned
    /// </summary>
    public int Count => Objects.Count;
}

/// <summary>
/// Options for uploading objects to S3
/// </summary>
public class UploadOptions
{
    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Storage class (STANDARD, REDUCED_REDUNDANCY, GLACIER, etc.)
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// Server-side encryption method
    /// </summary>
    public string? ServerSideEncryption { get; set; }

    /// <summary>
    /// Cache control header
    /// </summary>
    public string? CacheControl { get; set; }

    /// <summary>
    /// Content disposition header
    /// </summary>
    public string? ContentDisposition { get; set; }

    /// <summary>
    /// Make object publicly readable
    /// </summary>
    public bool MakePublic { get; set; }

    /// <summary>
    /// Creates default upload options
    /// </summary>
    public static UploadOptions Default()
    {
        return new UploadOptions
        {
            ContentType = "application/octet-stream",
            MakePublic = false
        };
    }
}

/// <summary>
/// Options for downloading objects from S3
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// Byte range start (for partial downloads)
    /// </summary>
    public long? RangeStart { get; set; }

    /// <summary>
    /// Byte range end (for partial downloads)
    /// </summary>
    public long? RangeEnd { get; set; }

    /// <summary>
    /// Version ID (for versioned buckets)
    /// </summary>
    public string? VersionId { get; set; }

    /// <summary>
    /// Creates default download options
    /// </summary>
    public static DownloadOptions Default()
    {
        return new DownloadOptions();
    }
}
