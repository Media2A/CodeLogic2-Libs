# CL.StorageS3

Amazon S3 and S3-compatible storage library for the **CodeLogic Framework**.

## Features

- ✅ **Full S3 Support**: Amazon S3, MinIO, DigitalOcean Spaces, Wasabi, and other S3-compatible services
- ✅ **Bucket Operations**: Create, delete, list, and manage buckets
- ✅ **Object Operations**: Upload, download, delete, copy, and list objects
- ✅ **Stream Support**: Upload and download large files efficiently with streams
- ✅ **Metadata Management**: Custom metadata, content types, and object information
- ✅ **Multiple Connections**: Support for multiple S3 services and buckets
- ✅ **Connection Pooling**: Efficient client connection management
- ✅ **Health Checks**: Automatic connection and bucket accessibility testing
- ✅ **Type Safety**: Strongly-typed models and operation results
- ✅ **Error Handling**: Comprehensive error handling with detailed messages
- ✅ **CodeLogic Integration**: Full integration with logging, configuration, and DI

## Installation

### 1. Add Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="path/to/CL.StorageS3/CL.StorageS3.csproj" />
</ItemGroup>
```

### 2. Configuration

Create `config/s3.json`:

```json
{
  "Connections": [
    {
      "ConnectionId": "Default",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "ServiceUrl": "https://s3.amazonaws.com",
      "PublicUrl": "https://s3.amazonaws.com",
      "Region": "us-east-1",
      "DefaultBucket": "my-bucket",
      "ForcePathStyle": false,
      "UseHttps": true,
      "TimeoutSeconds": 30,
      "MaxRetries": 3,
      "EnableLogging": false
    }
  ]
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionId` | string | "Default" | Unique identifier for this connection |
| `AccessKey` | string | "" | AWS access key or compatible service key |
| `SecretKey` | string | "" | AWS secret key or compatible service secret |
| `ServiceUrl` | string | "" | S3 service endpoint URL |
| `PublicUrl` | string | "" | Public URL for accessing objects |
| `Region` | string | "us-east-1" | AWS region or service region |
| `DefaultBucket` | string | "" | Default bucket name |
| `ForcePathStyle` | bool | true | Use path-style URLs (required for MinIO) |
| `UseHttps` | bool | true | Use SSL/TLS for connections |
| `TimeoutSeconds` | int | 30 | Request timeout in seconds |
| `MaxRetries` | int | 3 | Maximum retry attempts |
| `EnableLogging` | bool | false | Enable AWS SDK logging |

### S3-Compatible Services

**MinIO**:
```json
{
  "ServiceUrl": "http://localhost:9000",
  "ForcePathStyle": true
}
```

**DigitalOcean Spaces**:
```json
{
  "ServiceUrl": "https://nyc3.digitaloceanspaces.com",
  "Region": "nyc3",
  "ForcePathStyle": false
}
```

**Wasabi**:
```json
{
  "ServiceUrl": "https://s3.wasabisys.com",
  "Region": "us-east-1",
  "ForcePathStyle": false
}
```

## Usage

### Getting the Library Instance

```csharp
// Get library from LibraryManager
var s3Library = libraryManager.GetLibrary<StorageS3Library>();

// Get storage service for default connection
var storage = s3Library.GetStorageService();

// Get storage service for specific connection
var storage = s3Library.GetStorageService("MyConnection");
```

### Bucket Operations

#### Create Bucket

```csharp
var result = await storage.CreateBucketAsync("my-bucket");

if (result.Success)
{
    Console.WriteLine("Bucket created successfully");
}
```

#### Delete Bucket

```csharp
var result = await storage.DeleteBucketAsync("my-bucket");
```

#### List Buckets

```csharp
var result = await storage.ListBucketsAsync();

if (result.Success && result.Data != null)
{
    foreach (var bucket in result.Data)
    {
        Console.WriteLine($"Bucket: {bucket.Name}, Created: {bucket.CreationDate}");
    }
}
```

#### Check Bucket Existence

```csharp
bool exists = await storage.BucketExistsAsync("my-bucket");
```

### Object Upload Operations

#### Upload from Byte Array

```csharp
byte[] data = File.ReadAllBytes("document.pdf");

var result = await storage.PutObjectAsync(
    bucketName: "my-bucket",
    key: "documents/document.pdf",
    data: data,
    options: new UploadOptions
    {
        ContentType = "application/pdf",
        MakePublic = true,
        Metadata = new Dictionary<string, string>
        {
            ["author"] = "John Doe",
            ["version"] = "1.0"
        }
    }
);

if (result.Success && result.Data != null)
{
    Console.WriteLine($"Uploaded: {result.Data.PublicUrl}");
}
```

#### Upload from Stream

```csharp
using var fileStream = File.OpenRead("large-file.zip");

var result = await storage.PutObjectStreamAsync(
    bucketName: "my-bucket",
    key: "archives/large-file.zip",
    stream: fileStream,
    options: new UploadOptions
    {
        ContentType = "application/zip"
    }
);
```

#### Upload File Directly

```csharp
var result = await storage.PutObjectFileAsync(
    bucketName: "my-bucket",
    key: "images/photo.jpg",
    filePath: "C:/photos/photo.jpg",
    options: new UploadOptions
    {
        ContentType = "image/jpeg",
        CacheControl = "max-age=3600"
    }
);
```

### Object Download Operations

#### Download as Stream

```csharp
var result = await storage.GetObjectAsync("my-bucket", "documents/file.pdf");

if (result.Success && result.Data != null)
{
    using var stream = result.Data;
    // Process stream...
}
```

#### Download as Byte Array

```csharp
var result = await storage.GetObjectBytesAsync("my-bucket", "images/photo.jpg");

if (result.Success && result.Data != null)
{
    byte[] imageData = result.Data;
    // Process bytes...
}
```

#### Download to File

```csharp
var result = await storage.DownloadObjectFileAsync(
    bucketName: "my-bucket",
    key: "archives/backup.zip",
    filePath: "C:/downloads/backup.zip"
);
```

#### Partial Download (Range)

```csharp
var result = await storage.GetObjectAsync(
    bucketName: "my-bucket",
    key: "large-file.bin",
    options: new DownloadOptions
    {
        RangeStart = 0,
        RangeEnd = 1024 * 1024  // First 1MB
    }
);
```

### Object Management Operations

#### Delete Object

```csharp
var result = await storage.DeleteObjectAsync("my-bucket", "files/old-file.txt");
```

#### Copy Object

```csharp
var result = await storage.CopyObjectAsync(
    sourceBucket: "source-bucket",
    sourceKey: "files/original.txt",
    destinationBucket: "dest-bucket",
    destinationKey: "files/copy.txt"
);
```

#### Get Object Metadata

```csharp
var result = await storage.GetObjectMetadataAsync("my-bucket", "files/document.pdf");

if (result.Success && result.Data != null)
{
    var info = result.Data;
    Console.WriteLine($"Size: {info.Size} bytes");
    Console.WriteLine($"Content-Type: {info.ContentType}");
    Console.WriteLine($"Last Modified: {info.LastModified}");
    Console.WriteLine($"ETag: {info.ETag}");
}
```

#### Check Object Existence

```csharp
bool exists = await storage.ObjectExistsAsync("my-bucket", "files/check.txt");
```

### List Operations

#### List All Objects

```csharp
var result = await storage.ListObjectsAsync("my-bucket");

if (result.Success && result.Data != null)
{
    foreach (var obj in result.Data.Objects)
    {
        Console.WriteLine($"{obj.Key} - {obj.Size} bytes - {obj.LastModified}");
    }
}
```

#### List with Prefix Filter

```csharp
var result = await storage.ListObjectsAsync(
    bucketName: "my-bucket",
    prefix: "images/2024/",
    maxKeys: 100
);
```

#### Paginated Listing

```csharp
string? continuationToken = null;

do
{
    var result = await storage.ListObjectsAsync(
        bucketName: "my-bucket",
        maxKeys: 1000,
        continuationToken: continuationToken
    );

    if (result.Success && result.Data != null)
    {
        foreach (var obj in result.Data.Objects)
        {
            Console.WriteLine(obj.Key);
        }

        continuationToken = result.Data.NextContinuationToken;
    }
    else
    {
        break;
    }
}
while (continuationToken != null);
```

## Advanced Usage

### Multiple S3 Connections

```csharp
// Configure multiple connections in config/s3.json
{
  "Connections": [
    {
      "ConnectionId": "AWS",
      "ServiceUrl": "https://s3.amazonaws.com",
      "Region": "us-east-1"
    },
    {
      "ConnectionId": "MinIO",
      "ServiceUrl": "http://localhost:9000",
      "ForcePathStyle": true
    },
    {
      "ConnectionId": "DigitalOcean",
      "ServiceUrl": "https://nyc3.digitaloceanspaces.com",
      "Region": "nyc3"
    }
  ]
}

// Use different connections
var awsStorage = s3Library.GetStorageService("AWS");
var minioStorage = s3Library.GetStorageService("MinIO");
var doStorage = s3Library.GetStorageService("DigitalOcean");
```

### Dynamic Connection Registration

```csharp
var s3Library = libraryManager.GetLibrary<StorageS3Library>();

// Register new connection at runtime
var config = new S3Configuration
{
    ConnectionId = "Runtime",
    AccessKey = "key",
    SecretKey = "secret",
    ServiceUrl = "https://s3.example.com",
    DefaultBucket = "my-bucket"
};

s3Library.RegisterBucket(config);

// Use the new connection
var storage = s3Library.GetStorageService("Runtime");
```

### Upload Options

```csharp
var options = new UploadOptions
{
    ContentType = "image/jpeg",
    Metadata = new Dictionary<string, string>
    {
        ["uploaded-by"] = "user123",
        ["category"] = "photos"
    },
    StorageClass = "STANDARD_IA",  // Infrequent Access
    ServerSideEncryption = "AES256",
    CacheControl = "max-age=86400",
    ContentDisposition = "inline; filename=\"photo.jpg\"",
    MakePublic = false
};

var result = await storage.PutObjectAsync("bucket", "key", data, options);
```

### Error Handling

```csharp
var result = await storage.PutObjectAsync("bucket", "key", data);

if (result.Success)
{
    Console.WriteLine($"Upload successful: {result.Data?.PublicUrl}");
}
else
{
    Console.WriteLine($"Upload failed: {result.Error}");

    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception.Message}");
    }

    Console.WriteLine($"Status Code: {result.StatusCode}");
}
```

### Health Checks

```csharp
var s3Library = libraryManager.GetLibrary<StorageS3Library>();

// Check all connections
var health = await s3Library.HealthCheckAsync();

Console.WriteLine($"Healthy: {health.IsHealthy}");
Console.WriteLine($"Message: {health.Message}");

if (health.Details != null)
{
    Console.WriteLine($"Total Connections: {health.Details["total_connections"]}");
    Console.WriteLine($"Healthy Connections: {health.Details["healthy_connections"]}");
}
```

### Connection Manager

```csharp
var connectionManager = s3Library.GetConnectionManager();

// Get all connection IDs
var connections = connectionManager.GetConnectionIds();

// Test specific connection
bool isHealthy = await connectionManager.TestConnectionAsync("Default");

// Test bucket access
bool canAccess = await connectionManager.TestBucketAccessAsync("my-bucket", "Default");

// Close specific connection
connectionManager.CloseConnection("Default");
```

## Models

### OperationResult<T>

All operations return `OperationResult<T>`:

```csharp
public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public Exception? Exception { get; set; }
    public int StatusCode { get; set; }
}
```

### S3ObjectInfo

Information about an S3 object:

```csharp
public class S3ObjectInfo
{
    public string Key { get; set; }
    public string BucketName { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string? ETag { get; set; }
    public string? StorageClass { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public string? PublicUrl { get; set; }
}
```

### BucketInfo

Information about an S3 bucket:

```csharp
public class BucketInfo
{
    public string Name { get; set; }
    public DateTime CreationDate { get; set; }
    public string? Region { get; set; }
}
```

## Integration with CodeLogic

### Logging

All operations are automatically logged using CodeLogic's `ILogger`:

```csharp
// Logs are automatically created for:
- Connection initialization
- Bucket operations
- Object uploads/downloads
- Errors and warnings
```

### Configuration

Uses CodeLogic's `ConfigurationManager` for automatic configuration loading from `config/s3.json`.

### Dependency Injection

Fully integrated with CodeLogic's dependency injection system.

### Health Checks

Implements `ILibrary.HealthCheckAsync()` for monitoring S3 connection health.

## Best Practices

1. **Use Streams for Large Files**: Use `PutObjectStreamAsync` and `GetObjectAsync` for files larger than 10MB
2. **Set Content Types**: Always specify the correct `ContentType` in `UploadOptions`
3. **Handle Errors**: Always check `OperationResult.Success` before accessing `Data`
4. **Use Pagination**: When listing many objects, use pagination with continuation tokens
5. **Cache Connections**: The library automatically caches S3 clients for performance
6. **Test Connections**: Use health checks to verify connectivity before critical operations
7. **Close Streams**: Always dispose streams after download operations

## Troubleshooting

### Connection Timeout

Increase `TimeoutSeconds` in configuration:

```json
{
  "TimeoutSeconds": 60
}
```

### Path Style Issues (MinIO)

Enable `ForcePathStyle` for MinIO and compatible services:

```json
{
  "ForcePathStyle": true
}
```

### SSL Certificate Errors

For development with self-signed certificates, consider using HTTP:

```json
{
  "UseHttps": false,
  "ServiceUrl": "http://localhost:9000"
}
```

### Access Denied

Verify your access key, secret key, and bucket permissions.

## Dependencies

- **AWSSDK.S3**: 3.7.400+ (Amazon S3 SDK for .NET)
- **CL.Core**: 2.0.0+ (CodeLogic core abstractions)
- **CodeLogic**: 2.0.0+ (Main framework)

## Version

Current version: **2.0.0**

## License

Copyright © Media2A 2024

## Support

For issues and feature requests, please use the project's issue tracker.
