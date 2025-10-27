using System.Collections.Concurrent;

namespace CL.Core.Utilities.FileHandling;

/// <summary>
/// Provides thread-safe file system operations
/// </summary>
public static class FileSystem
{
    private static readonly SemaphoreSlim _fileWriteSemaphore = new(1, 1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    /// <summary>
    /// Combines multiple path segments and normalizes directory separators for the current platform
    /// </summary>
    public static string NormalizePath(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return string.Empty;

        var validPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        if (validPaths.Length == 0)
            return string.Empty;

        string combinedPath = Path.Combine(validPaths);
        return Path.DirectorySeparatorChar == '/'
            ? combinedPath.Replace('\\', '/')
            : combinedPath.Replace('/', '\\');
    }

    /// <summary>
    /// Gets all files in a directory matching a pattern
    /// </summary>
    public static List<string> GetFilesInDirectory(string directoryPath, string pattern = "*", bool recursive = true)
    {
        var fileList = new List<string>();
        try
        {
            if (!Directory.Exists(directoryPath))
                return fileList;

            var directory = new DirectoryInfo(directoryPath);
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in directory.GetFiles(pattern, searchOption))
            {
                fileList.Add(file.FullName);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Error getting files in directory: {directoryPath}", ex);
        }
        return fileList;
    }

    /// <summary>
    /// Asynchronously creates a new file at the specified path
    /// </summary>
    public static async Task<bool> CreateFileAsync(string path, bool overwrite = true, bool createPath = true)
    {
        try
        {
            if (createPath)
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            if (File.Exists(path))
            {
                if (!overwrite)
                    return true;

                File.Delete(path);
            }

            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await fs.FlushAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new IOException($"Error creating file: {path}", ex);
        }
    }

    /// <summary>
    /// Asynchronously reads the entire contents of a file
    /// </summary>
    public static async Task<string> ReadFileAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new IOException($"Error reading file: {path}", ex);
        }
    }

    /// <summary>
    /// Asynchronously reads file as bytes
    /// </summary>
    public static async Task<byte[]> ReadFileBytesAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            return await File.ReadAllBytesAsync(path);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new IOException($"Error reading file bytes: {path}", ex);
        }
    }

    /// <summary>
    /// Asynchronously writes content to a file with thread safety
    /// </summary>
    public static async Task<bool> WriteFileAsync(string path, string content, bool createPath = true, bool append = false)
    {
        try
        {
            if (createPath)
            {
                await EnsureFileExistsAsync(path);
            }

            var fileLock = _fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();

            try
            {
                if (append)
                {
                    await File.AppendAllTextAsync(path, content);
                }
                else
                {
                    await File.WriteAllTextAsync(path, content);
                }
            }
            finally
            {
                fileLock.Release();
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new IOException($"Error writing to file: {path}", ex);
        }
    }

    /// <summary>
    /// Asynchronously writes bytes to a file
    /// </summary>
    public static async Task<bool> WriteFileBytesAsync(string path, byte[] bytes, bool createPath = true)
    {
        try
        {
            if (createPath)
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            await File.WriteAllBytesAsync(path, bytes);
            return true;
        }
        catch (Exception ex)
        {
            throw new IOException($"Error writing bytes to file: {path}", ex);
        }
    }

    /// <summary>
    /// Deletes a file at the specified path
    /// </summary>
    public static Task<bool> DeleteFileAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            throw new IOException($"Error deleting file: {path}", ex);
        }
    }

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    public static bool FileExists(string path) => File.Exists(path);

    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    public static bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    /// Ensures a file exists, creating it if necessary
    /// </summary>
    public static async Task EnsureFileExistsAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            string? directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await CreateFileAsync(filePath, overwrite: false, createPath: false);
        }
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    public static long GetFileSize(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }

    /// <summary>
    /// Gets the file's last modified time
    /// </summary>
    public static DateTime GetLastModified(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue;
    }
}
