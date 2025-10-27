# CL.StorageS3 Examples

Comprehensive examples for using the CL.StorageS3 library with Amazon S3 and S3-compatible storage services.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Bucket Operations](#bucket-operations)
3. [Upload Operations](#upload-operations)
4. [Download Operations](#download-operations)
5. [Object Management](#object-management)
6. [List Operations](#list-operations)
7. [Advanced Scenarios](#advanced-scenarios)
8. [Real-World Examples](#real-world-examples)

## Getting Started

### Example 1: Basic Setup

```csharp
using CodeLogic;
using CL.StorageS3;
using CL.StorageS3.Models;

// Get the S3 library from LibraryManager
var s3Library = libraryManager.GetLibrary<StorageS3Library>();

// Get storage service for default connection
var storage = s3Library.GetStorageService();

// Verify connection
var health = await s3Library.HealthCheckAsync();
Console.WriteLine($"S3 Connection: {health.Message}");
```

### Example 2: Multiple Connection Setup

```csharp
// Get different storage services
var awsStorage = s3Library.GetStorageService("AWS");
var minioStorage = s3Library.GetStorageService("MinIO");
var doStorage = s3Library.GetStorageService("DigitalOcean");

// Test each connection
var awsHealth = await s3Library.HealthCheckAsync();
Console.WriteLine($"AWS: {awsHealth.Message}");
```

## Bucket Operations

### Example 3: Create Bucket with Error Handling

```csharp
async Task<bool> CreateBucketSafely(string bucketName)
{
    var storage = s3Library.GetStorageService();

    // Check if bucket already exists
    bool exists = await storage.BucketExistsAsync(bucketName);

    if (exists)
    {
        Console.WriteLine($"Bucket '{bucketName}' already exists");
        return true;
    }

    // Create bucket
    var result = await storage.CreateBucketAsync(bucketName);

    if (result.Success)
    {
        Console.WriteLine($"✓ Bucket '{bucketName}' created successfully");
        return true;
    }
    else
    {
        Console.WriteLine($"✗ Failed to create bucket: {result.Error}");
        return false;
    }
}
```

### Example 4: List All Buckets with Details

```csharp
async Task ListAllBucketsWithDetails()
{
    var storage = s3Library.GetStorageService();

    var result = await storage.ListBucketsAsync();

    if (result.Success && result.Data != null)
    {
        Console.WriteLine($"Found {result.Data.Count} buckets:\n");

        foreach (var bucket in result.Data.OrderBy(b => b.Name))
        {
            Console.WriteLine($"📦 {bucket.Name}");
            Console.WriteLine($"   Created: {bucket.CreationDate:yyyy-MM-dd HH:mm:ss}");

            if (!string.IsNullOrEmpty(bucket.Region))
            {
                Console.WriteLine($"   Region: {bucket.Region}");
            }

            Console.WriteLine();
        }
    }
    else
    {
        Console.WriteLine($"Failed to list buckets: {result.Error}");
    }
}
```

### Example 5: Delete Bucket with Confirmation

```csharp
async Task<bool> DeleteBucketWithConfirmation(string bucketName)
{
    var storage = s3Library.GetStorageService();

    // Check if bucket exists
    if (!await storage.BucketExistsAsync(bucketName))
    {
        Console.WriteLine($"Bucket '{bucketName}' does not exist");
        return false;
    }

    // Check if bucket is empty
    var listResult = await storage.ListObjectsAsync(bucketName, maxKeys: 1);

    if (listResult.Success && listResult.Data!.Count > 0)
    {
        Console.WriteLine($"Bucket '{bucketName}' is not empty. Delete all objects first.");
        return false;
    }

    // Delete bucket
    var result = await storage.DeleteBucketAsync(bucketName);

    if (result.Success)
    {
        Console.WriteLine($"✓ Bucket '{bucketName}' deleted successfully");
        return true;
    }
    else
    {
        Console.WriteLine($"✗ Failed to delete bucket: {result.Error}");
        return false;
    }
}
```

## Upload Operations

### Example 6: Upload Image with Metadata

```csharp
async Task<string?> UploadImageWithMetadata(string localPath, string bucketName)
{
    var storage = s3Library.GetStorageService();

    // Read image file
    byte[] imageData = await File.ReadAllBytesAsync(localPath);

    // Determine content type
    string extension = Path.GetExtension(localPath).ToLower();
    string contentType = extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    // Create S3 key
    string fileName = Path.GetFileName(localPath);
    string key = $"images/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";

    // Upload with metadata
    var result = await storage.PutObjectAsync(
        bucketName: bucketName,
        key: key,
        data: imageData,
        options: new UploadOptions
        {
            ContentType = contentType,
            MakePublic = true,
            CacheControl = "max-age=31536000, public",
            Metadata = new Dictionary<string, string>
            {
                ["uploaded-at"] = DateTime.UtcNow.ToString("o"),
                ["original-filename"] = fileName,
                ["size-bytes"] = imageData.Length.ToString()
            }
        }
    );

    if (result.Success && result.Data != null)
    {
        Console.WriteLine($"✓ Image uploaded: {result.Data.PublicUrl}");
        return result.Data.PublicUrl;
    }
    else
    {
        Console.WriteLine($"✗ Upload failed: {result.Error}");
        return null;
    }
}
```

### Example 7: Upload Large File with Stream

```csharp
async Task<bool> UploadLargeFile(string localPath, string bucketName, string s3Key)
{
    var storage = s3Library.GetStorageService();

    // Get file info
    var fileInfo = new FileInfo(localPath);
    Console.WriteLine($"Uploading {fileInfo.Name} ({fileInfo.Length / 1024 / 1024} MB)...");

    try
    {
        using var fileStream = File.OpenRead(localPath);

        var result = await storage.PutObjectStreamAsync(
            bucketName: bucketName,
            key: s3Key,
            stream: fileStream,
            options: new UploadOptions
            {
                ContentType = GetMimeType(localPath),
                Metadata = new Dictionary<string, string>
                {
                    ["file-size"] = fileInfo.Length.ToString(),
                    ["upload-date"] = DateTime.UtcNow.ToString("o")
                }
            }
        );

        if (result.Success)
        {
            Console.WriteLine($"✓ Upload completed: {s3Key}");
            return true;
        }
        else
        {
            Console.WriteLine($"✗ Upload failed: {result.Error}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Upload error: {ex.Message}");
        return false;
    }
}

string GetMimeType(string filePath)
{
    string extension = Path.GetExtension(filePath).ToLower();
    return extension switch
    {
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        ".txt" => "text/plain",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".mp4" => "video/mp4",
        ".mp3" => "audio/mpeg",
        _ => "application/octet-stream"
    };
}
```

### Example 8: Batch Upload Multiple Files

```csharp
async Task<int> BatchUploadFiles(string[] localPaths, string bucketName, string s3Prefix)
{
    var storage = s3Library.GetStorageService();
    int successCount = 0;
    int failureCount = 0;

    Console.WriteLine($"Uploading {localPaths.Length} files...\n");

    foreach (var localPath in localPaths)
    {
        string fileName = Path.GetFileName(localPath);
        string s3Key = $"{s3Prefix}/{fileName}";

        var result = await storage.PutObjectFileAsync(
            bucketName: bucketName,
            key: s3Key,
            filePath: localPath,
            options: new UploadOptions
            {
                ContentType = GetMimeType(localPath)
            }
        );

        if (result.Success)
        {
            successCount++;
            Console.WriteLine($"✓ {fileName}");
        }
        else
        {
            failureCount++;
            Console.WriteLine($"✗ {fileName}: {result.Error}");
        }
    }

    Console.WriteLine($"\nResults: {successCount} succeeded, {failureCount} failed");
    return successCount;
}
```

## Download Operations

### Example 9: Download File with Progress

```csharp
async Task<bool> DownloadFileWithProgress(string bucketName, string key, string localPath)
{
    var storage = s3Library.GetStorageService();

    // Get object metadata first to show size
    var metaResult = await storage.GetObjectMetadataAsync(bucketName, key);

    if (!metaResult.Success)
    {
        Console.WriteLine($"✗ Failed to get metadata: {metaResult.Error}");
        return false;
    }

    long totalSize = metaResult.Data!.Size;
    Console.WriteLine($"Downloading {key} ({totalSize / 1024 / 1024} MB)...");

    // Download
    var result = await storage.DownloadObjectFileAsync(bucketName, key, localPath);

    if (result.Success)
    {
        Console.WriteLine($"✓ Downloaded to: {localPath}");
        return true;
    }
    else
    {
        Console.WriteLine($"✗ Download failed: {result.Error}");
        return false;
    }
}
```

### Example 10: Download and Process Stream

```csharp
async Task<string> DownloadAndProcessTextFile(string bucketName, string key)
{
    var storage = s3Library.GetStorageService();

    var result = await storage.GetObjectAsync(bucketName, key);

    if (result.Success && result.Data != null)
    {
        using var stream = result.Data;
        using var reader = new StreamReader(stream);

        string content = await reader.ReadToEndAsync();

        Console.WriteLine($"✓ Downloaded {content.Length} characters");
        return content;
    }
    else
    {
        Console.WriteLine($"✗ Download failed: {result.Error}");
        return string.Empty;
    }
}
```

### Example 11: Download Byte Array

```csharp
async Task<byte[]?> DownloadImageBytes(string bucketName, string key)
{
    var storage = s3Library.GetStorageService();

    var result = await storage.GetObjectBytesAsync(bucketName, key);

    if (result.Success && result.Data != null)
    {
        Console.WriteLine($"✓ Downloaded {result.Data.Length} bytes");
        return result.Data;
    }
    else
    {
        Console.WriteLine($"✗ Download failed: {result.Error}");
        return null;
    }
}
```

### Example 12: Partial Download (Range Request)

```csharp
async Task<byte[]?> DownloadFileRange(string bucketName, string key, long start, long end)
{
    var storage = s3Library.GetStorageService();

    Console.WriteLine($"Downloading bytes {start}-{end} from {key}...");

    var result = await storage.GetObjectAsync(
        bucketName: bucketName,
        key: key,
        options: new DownloadOptions
        {
            RangeStart = start,
            RangeEnd = end
        }
    );

    if (result.Success && result.Data != null)
    {
        using var stream = result.Data;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();
        Console.WriteLine($"✓ Downloaded {bytes.Length} bytes");
        return bytes;
    }
    else
    {
        Console.WriteLine($"✗ Download failed: {result.Error}");
        return null;
    }
}
```

## Object Management

### Example 13: Copy Object Between Buckets

```csharp
async Task<bool> CopyObjectBetweenBuckets(
    string sourceBucket,
    string sourceKey,
    string destBucket,
    string destKey)
{
    var storage = s3Library.GetStorageService();

    Console.WriteLine($"Copying {sourceBucket}/{sourceKey} -> {destBucket}/{destKey}...");

    var result = await storage.CopyObjectAsync(
        sourceBucket: sourceBucket,
        sourceKey: sourceKey,
        destinationBucket: destBucket,
        destinationKey: destKey
    );

    if (result.Success)
    {
        Console.WriteLine("✓ Copy completed");
        return true;
    }
    else
    {
        Console.WriteLine($"✗ Copy failed: {result.Error}");
        return false;
    }
}
```

### Example 14: Delete Multiple Objects

```csharp
async Task<int> DeleteMultipleObjects(string bucketName, string[] keys)
{
    var storage = s3Library.GetStorageService();
    int deletedCount = 0;

    Console.WriteLine($"Deleting {keys.Length} objects...\n");

    foreach (var key in keys)
    {
        var result = await storage.DeleteObjectAsync(bucketName, key);

        if (result.Success)
        {
            deletedCount++;
            Console.WriteLine($"✓ Deleted: {key}");
        }
        else
        {
            Console.WriteLine($"✗ Failed to delete {key}: {result.Error}");
        }
    }

    Console.WriteLine($"\nDeleted {deletedCount} of {keys.Length} objects");
    return deletedCount;
}
```

### Example 15: Get and Display Object Metadata

```csharp
async Task DisplayObjectMetadata(string bucketName, string key)
{
    var storage = s3Library.GetStorageService();

    var result = await storage.GetObjectMetadataAsync(bucketName, key);

    if (result.Success && result.Data != null)
    {
        var obj = result.Data;

        Console.WriteLine($"Object: {obj.Key}");
        Console.WriteLine($"Bucket: {obj.BucketName}");
        Console.WriteLine($"Size: {obj.Size:N0} bytes ({obj.Size / 1024.0 / 1024.0:F2} MB)");
        Console.WriteLine($"Content-Type: {obj.ContentType}");
        Console.WriteLine($"Last Modified: {obj.LastModified:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"ETag: {obj.ETag}");
        Console.WriteLine($"Storage Class: {obj.StorageClass}");

        if (obj.Metadata != null && obj.Metadata.Any())
        {
            Console.WriteLine("\nCustom Metadata:");
            foreach (var kvp in obj.Metadata)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        if (!string.IsNullOrEmpty(obj.PublicUrl))
        {
            Console.WriteLine($"\nPublic URL: {obj.PublicUrl}");
        }
    }
    else
    {
        Console.WriteLine($"✗ Failed to get metadata: {result.Error}");
    }
}
```

## List Operations

### Example 16: List Objects with Filtering

```csharp
async Task ListObjectsInFolder(string bucketName, string folderPrefix)
{
    var storage = s3Library.GetStorageService();

    var result = await storage.ListObjectsAsync(
        bucketName: bucketName,
        prefix: folderPrefix,
        maxKeys: 100
    );

    if (result.Success && result.Data != null)
    {
        Console.WriteLine($"Objects in {bucketName}/{folderPrefix}:\n");

        foreach (var obj in result.Data.Objects)
        {
            Console.WriteLine($"📄 {obj.Key}");
            Console.WriteLine($"   Size: {obj.Size:N0} bytes");
            Console.WriteLine($"   Modified: {obj.LastModified:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {result.Data.Count} objects");
    }
    else
    {
        Console.WriteLine($"✗ Failed to list objects: {result.Error}");
    }
}
```

### Example 17: Paginated Object Listing

```csharp
async Task<List<S3ObjectInfo>> ListAllObjects(string bucketName, string? prefix = null)
{
    var storage = s3Library.GetStorageService();
    var allObjects = new List<S3ObjectInfo>();
    string? continuationToken = null;
    int pageNumber = 1;

    do
    {
        var result = await storage.ListObjectsAsync(
            bucketName: bucketName,
            prefix: prefix,
            maxKeys: 1000,
            continuationToken: continuationToken
        );

        if (result.Success && result.Data != null)
        {
            allObjects.AddRange(result.Data.Objects);

            Console.WriteLine($"Page {pageNumber}: {result.Data.Count} objects");

            continuationToken = result.Data.NextContinuationToken;
            pageNumber++;
        }
        else
        {
            Console.WriteLine($"✗ Failed to list objects: {result.Error}");
            break;
        }
    }
    while (continuationToken != null);

    Console.WriteLine($"\nTotal objects: {allObjects.Count}");
    return allObjects;
}
```

### Example 18: Search for Objects by Pattern

```csharp
async Task<List<S3ObjectInfo>> SearchObjectsByPattern(
    string bucketName,
    string searchPattern,
    string? prefix = null)
{
    var storage = s3Library.GetStorageService();
    var matchingObjects = new List<S3ObjectInfo>();

    // Get all objects
    var allObjects = await ListAllObjects(bucketName, prefix);

    // Filter by pattern
    foreach (var obj in allObjects)
    {
        if (obj.Key.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
        {
            matchingObjects.Add(obj);
        }
    }

    Console.WriteLine($"Found {matchingObjects.Count} objects matching '{searchPattern}'");

    return matchingObjects;
}
```

## Advanced Scenarios

### Example 19: Backup Files to S3

```csharp
async Task BackupDirectoryToS3(string localDirectory, string bucketName, string s3Prefix)
{
    var storage = s3Library.GetStorageService();

    // Get all files recursively
    var files = Directory.GetFiles(localDirectory, "*", SearchOption.AllDirectories);

    Console.WriteLine($"Backing up {files.Length} files to S3...\n");

    int successCount = 0;
    int failureCount = 0;

    foreach (var localPath in files)
    {
        // Create relative path for S3 key
        string relativePath = Path.GetRelativePath(localDirectory, localPath);
        string s3Key = $"{s3Prefix}/{relativePath}".Replace("\\", "/");

        var result = await storage.PutObjectFileAsync(
            bucketName: bucketName,
            key: s3Key,
            filePath: localPath,
            options: new UploadOptions
            {
                ContentType = GetMimeType(localPath),
                Metadata = new Dictionary<string, string>
                {
                    ["backup-date"] = DateTime.UtcNow.ToString("o"),
                    ["original-path"] = localPath
                }
            }
        );

        if (result.Success)
        {
            successCount++;
            Console.WriteLine($"✓ {relativePath}");
        }
        else
        {
            failureCount++;
            Console.WriteLine($"✗ {relativePath}: {result.Error}");
        }
    }

    Console.WriteLine($"\nBackup completed: {successCount} succeeded, {failureCount} failed");
}
```

### Example 20: Restore Files from S3

```csharp
async Task RestoreBackupFromS3(string bucketName, string s3Prefix, string localDirectory)
{
    var storage = s3Library.GetStorageService();

    // List all objects with prefix
    var allObjects = await ListAllObjects(bucketName, s3Prefix);

    Console.WriteLine($"Restoring {allObjects.Count} files from S3...\n");

    int successCount = 0;
    int failureCount = 0;

    foreach (var obj in allObjects)
    {
        // Remove prefix to get relative path
        string relativePath = obj.Key.Substring(s3Prefix.Length + 1);
        string localPath = Path.Combine(localDirectory, relativePath);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var result = await storage.DownloadObjectFileAsync(
            bucketName: bucketName,
            key: obj.Key,
            filePath: localPath
        );

        if (result.Success)
        {
            successCount++;
            Console.WriteLine($"✓ {relativePath}");
        }
        else
        {
            failureCount++;
            Console.WriteLine($"✗ {relativePath}: {result.Error}");
        }
    }

    Console.WriteLine($"\nRestore completed: {successCount} succeeded, {failureCount} failed");
}
```

### Example 21: Sync Local Directory to S3

```csharp
async Task SyncDirectoryToS3(string localDirectory, string bucketName, string s3Prefix)
{
    var storage = s3Library.GetStorageService();

    // Get local files
    var localFiles = Directory.GetFiles(localDirectory, "*", SearchOption.AllDirectories)
        .Select(f => Path.GetRelativePath(localDirectory, f).Replace("\\", "/"))
        .ToHashSet();

    // Get S3 objects
    var s3Objects = await ListAllObjects(bucketName, s3Prefix);
    var s3Keys = s3Objects
        .Select(o => o.Key.Substring(s3Prefix.Length + 1))
        .ToHashSet();

    // Files to upload (new or modified)
    var filesToUpload = localFiles.ToList();

    // Files to delete from S3 (no longer exist locally)
    var keysToDelete = s3Keys.Except(localFiles).ToList();

    Console.WriteLine($"Sync Plan:");
    Console.WriteLine($"  Upload: {filesToUpload.Count} files");
    Console.WriteLine($"  Delete: {keysToDelete.Count} files\n");

    // Upload files
    foreach (var relativePath in filesToUpload)
    {
        string localPath = Path.Combine(localDirectory, relativePath);
        string s3Key = $"{s3Prefix}/{relativePath}";

        var result = await storage.PutObjectFileAsync(bucketName, s3Key, localPath);

        if (result.Success)
        {
            Console.WriteLine($"✓ Uploaded: {relativePath}");
        }
        else
        {
            Console.WriteLine($"✗ Upload failed: {relativePath}");
        }
    }

    // Delete removed files
    foreach (var relativePath in keysToDelete)
    {
        string s3Key = $"{s3Prefix}/{relativePath}";

        var result = await storage.DeleteObjectAsync(bucketName, s3Key);

        if (result.Success)
        {
            Console.WriteLine($"✓ Deleted: {relativePath}");
        }
        else
        {
            Console.WriteLine($"✗ Delete failed: {relativePath}");
        }
    }

    Console.WriteLine("\nSync completed");
}
```

## Real-World Examples

### Example 22: Image Processing Pipeline

```csharp
async Task ProcessAndUploadImages(string[] imagePaths, string bucketName)
{
    var storage = s3Library.GetStorageService();

    foreach (var imagePath in imagePaths)
    {
        try
        {
            // Read original image
            byte[] originalData = await File.ReadAllBytesAsync(imagePath);

            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            string extension = Path.GetExtension(imagePath);

            // Upload original
            string originalKey = $"images/originals/{fileName}{extension}";
            await storage.PutObjectAsync(bucketName, originalKey, originalData,
                new UploadOptions
                {
                    ContentType = "image/jpeg",
                    Metadata = new Dictionary<string, string>
                    {
                        ["type"] = "original",
                        ["processing-date"] = DateTime.UtcNow.ToString("o")
                    }
                });

            Console.WriteLine($"✓ Uploaded original: {fileName}");

            // In a real app, you would resize/process images here
            // and upload thumbnails/variants
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to process {imagePath}: {ex.Message}");
        }
    }
}
```

### Example 23: Log File Archival

```csharp
async Task ArchiveOldLogs(string logDirectory, string bucketName, int daysOld = 30)
{
    var storage = s3Library.GetStorageService();
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

    var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories)
        .Where(f => File.GetLastWriteTimeUtc(f) < cutoffDate)
        .ToArray();

    Console.WriteLine($"Archiving {logFiles.Length} log files older than {daysOld} days...\n");

    foreach (var logPath in logFiles)
    {
        string fileName = Path.GetFileName(logPath);
        string s3Key = $"logs/archive/{cutoffDate:yyyy/MM}/{fileName}";

        // Upload to S3
        var uploadResult = await storage.PutObjectFileAsync(
            bucketName: bucketName,
            key: s3Key,
            filePath: logPath,
            options: new UploadOptions
            {
                ContentType = "text/plain",
                StorageClass = "GLACIER",  // Long-term archive storage
                Metadata = new Dictionary<string, string>
                {
                    ["archived-date"] = DateTime.UtcNow.ToString("o"),
                    ["original-path"] = logPath
                }
            }
        );

        if (uploadResult.Success)
        {
            // Delete local file after successful upload
            File.Delete(logPath);
            Console.WriteLine($"✓ Archived and deleted: {fileName}");
        }
        else
        {
            Console.WriteLine($"✗ Failed to archive: {fileName}");
        }
    }
}
```

### Example 24: Multi-Tenant File Storage

```csharp
class MultiTenantFileManager
{
    private readonly S3StorageService _storage;
    private readonly string _bucketName;

    public MultiTenantFileManager(StorageS3Library s3Library, string bucketName)
    {
        _storage = s3Library.GetStorageService()!;
        _bucketName = bucketName;
    }

    public async Task<string?> UploadUserFile(string userId, string fileName, byte[] data)
    {
        string s3Key = $"users/{userId}/files/{fileName}";

        var result = await _storage.PutObjectAsync(
            bucketName: _bucketName,
            key: s3Key,
            data: data,
            options: new UploadOptions
            {
                ContentType = GetMimeType(fileName),
                Metadata = new Dictionary<string, string>
                {
                    ["user-id"] = userId,
                    ["upload-date"] = DateTime.UtcNow.ToString("o"),
                    ["file-name"] = fileName
                }
            }
        );

        return result.Success ? result.Data?.PublicUrl : null;
    }

    public async Task<List<S3ObjectInfo>> GetUserFiles(string userId)
    {
        string prefix = $"users/{userId}/files/";

        var result = await _storage.ListObjectsAsync(_bucketName, prefix);

        return result.Success && result.Data != null
            ? result.Data.Objects
            : new List<S3ObjectInfo>();
    }

    public async Task<byte[]?> DownloadUserFile(string userId, string fileName)
    {
        string s3Key = $"users/{userId}/files/{fileName}";

        var result = await _storage.GetObjectBytesAsync(_bucketName, s3Key);

        return result.Success ? result.Data : null;
    }

    public async Task<bool> DeleteUserFile(string userId, string fileName)
    {
        string s3Key = $"users/{userId}/files/{fileName}";

        var result = await _storage.DeleteObjectAsync(_bucketName, s3Key);

        return result.Success;
    }
}
```

## Helper Functions

```csharp
// MIME type detection
string GetMimeType(string fileName)
{
    string extension = Path.GetExtension(fileName).ToLower();
    return extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".zip" => "application/zip",
        ".mp4" => "video/mp4",
        ".mp3" => "audio/mpeg",
        _ => "application/octet-stream"
    };
}

// Format file size
string FormatFileSize(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = bytes;
    int order = 0;

    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }

    return $"{len:0.##} {sizes[order]}";
}
```
