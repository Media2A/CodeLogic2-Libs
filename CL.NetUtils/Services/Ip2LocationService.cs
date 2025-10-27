using System.Net;
using System.Net.Http.Headers;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using CL.NetUtils.Models;
using CodeLogic.Abstractions;

namespace CL.NetUtils.Services;

/// <summary>
/// Provides IP geolocation services using MaxMind GeoLite2 database
/// </summary>
public class Ip2LocationService
{
    private readonly Ip2LocationConfiguration _config;
    private readonly ILogger _logger;
    private DatabaseReader? _databaseReader;
    private string _databasePath = string.Empty;
    private readonly object _databaseLock = new object();

    /// <summary>
    /// Initializes a new instance of the Ip2LocationService class
    /// </summary>
    public Ip2LocationService(Ip2LocationConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the service and ensures the database is available
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _databasePath = Path.Combine(_config.DatabaseDirectory, $"{_config.DatabaseType}.mmdb");

        if (!File.Exists(_databasePath))
        {
            _logger.Info("IP geolocation database not found, downloading...");
            await DownloadDatabaseAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!File.Exists(_databasePath))
        {
            throw new InvalidOperationException($"Failed to obtain GeoIP database at {_databasePath}");
        }

        lock (_databaseLock)
        {
            _databaseReader = new DatabaseReader(_databasePath);
        }

        _logger.Info($"IP geolocation service initialized with database: {_databasePath}");
    }

    /// <summary>
    /// Looks up geolocation information for an IP address
    /// </summary>
    public async Task<IpLocationResult> LookupIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                _logger.Warning($"Invalid IP address: {ipAddress}");
                return IpLocationResult.Error(ipAddress, "Invalid IP address format");
            }

            if (_databaseReader == null)
            {
                _logger.Warning("Database reader not initialized");
                return IpLocationResult.Error(ipAddress, "IP geolocation service not initialized");
            }

            lock (_databaseLock)
            {
                var response = _databaseReader.City(ip);

                return new IpLocationResult
                {
                    IpAddress = ipAddress,
                    CountryName = response?.Country?.Name,
                    CountryCode = response?.Country?.IsoCode,
                    CityName = response?.City?.Name,
                    SubdivisionName = response?.MostSpecificSubdivision?.Name,
                    PostalCode = response?.Postal?.Code,
                    Latitude = response?.Location?.Latitude,
                    Longitude = response?.Location?.Longitude,
                    TimeZone = response?.Location?.TimeZone,
                    Isp = response?.Traits?.Isp
                };
            }
        }
        catch (AddressNotFoundException)
        {
            _logger.Debug($"IP address '{ipAddress}' not found in geolocation database");
            return IpLocationResult.NotFound(ipAddress);
        }
        catch (GeoIP2Exception ex)
        {
            _logger.Warning($"GeoIP2 error for {ipAddress}: {ex.Message}");
            return IpLocationResult.Error(ipAddress, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during IP geolocation lookup for {ipAddress}", ex);
            return IpLocationResult.Error(ipAddress, ex.Message);
        }
    }

    /// <summary>
    /// Downloads and extracts the MaxMind GeoLite2 database
    /// </summary>
    public async Task DownloadDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_config.AccountID == 0 || string.IsNullOrWhiteSpace(_config.LicenseKey))
            {
                _logger.Warning("MaxMind credentials not configured. Skipping database download.");
                return;
            }

            // Create directories
            Directory.CreateDirectory(_config.DatabaseDirectory);
            var tmpDir = Path.Combine(_config.DatabaseDirectory, "tmp");
            Directory.CreateDirectory(tmpDir);

            var downloadPath = Path.Combine(tmpDir, "database.tar.gz");

            _logger.Info($"Downloading GeoLite2 database from {_config.DownloadUrl}...");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            // Create basic auth header
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{_config.AccountID}:{_config.LicenseKey}"));

            using var request = new HttpRequestMessage(HttpMethod.Get, _config.DownloadUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Error("GeoIP database download returned 404. Check credentials and URL.");
                return;
            }

            response.EnsureSuccessStatusCode();

            // Download to temporary file
            await using (var fileStream = File.Create(downloadPath))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            _logger.Info("Database downloaded, extracting...");

            // Extract on thread pool
            await Task.Run(
                () => ExtractDatabase(downloadPath, _config.DatabaseDirectory),
                cancellationToken).ConfigureAwait(false);

            // Clean up temporary files
            try
            {
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);

                var extractTmpDir = Path.Combine(_config.DatabaseDirectory, "GeoLite2-City");
                if (Directory.Exists(extractTmpDir))
                    Directory.Delete(extractTmpDir, true);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to clean up temporary files: {ex.Message}");
            }

            _logger.Info("GeoIP database downloaded and extracted successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Database download was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Failed to download GeoIP database: network error", ex);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to download GeoIP database", ex);
        }
    }

    /// <summary>
    /// Closes and releases the database reader
    /// </summary>
    public void Dispose()
    {
        lock (_databaseLock)
        {
            _databaseReader?.Dispose();
            _databaseReader = null;
        }
    }

    /// <summary>
    /// Extracts a tar.gz archive
    /// </summary>
    private static void ExtractDatabase(string archivePath, string extractionPath)
    {
        using var fileStream = File.OpenRead(archivePath);
        using var reader = ReaderFactory.Open(fileStream);

        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                var extractOptions = new ExtractionOptions
                {
                    ExtractFullPath = false,
                    Overwrite = true
                };
                reader.WriteEntryToDirectory(extractionPath, extractOptions);
            }
        }
    }
}
