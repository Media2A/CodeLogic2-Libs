namespace CL.NetUtils.Models;

/// <summary>
/// Result of a DNSBL check
/// </summary>
public record DnsblCheckResult
{
    /// <summary>
    /// Gets the IP address that was checked
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Gets whether the IP is blacklisted
    /// </summary>
    public required bool IsBlacklisted { get; init; }

    /// <summary>
    /// Gets the DNSBL service that matched (if blacklisted)
    /// </summary>
    public string? MatchedService { get; init; }

    /// <summary>
    /// Gets the IP address type (IPv4 or IPv6)
    /// </summary>
    public required IpAddressType AddressType { get; init; }

    /// <summary>
    /// Gets any error message if the check failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp of the check
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a result indicating the IP is blacklisted
    /// </summary>
    public static DnsblCheckResult Blacklisted(string ip, IpAddressType type, string service) =>
        new() { IpAddress = ip, IsBlacklisted = true, MatchedService = service, AddressType = type };

    /// <summary>
    /// Creates a result indicating the IP is not blacklisted
    /// </summary>
    public static DnsblCheckResult NotBlacklisted(string ip, IpAddressType type) =>
        new() { IpAddress = ip, IsBlacklisted = false, AddressType = type };

    /// <summary>
    /// Creates an error result for failed DNSBL checks
    /// </summary>
    public static DnsblCheckResult Error(string ip, IpAddressType type, string error) =>
        new() { IpAddress = ip, IsBlacklisted = false, AddressType = type, ErrorMessage = error };
}

/// <summary>
/// Result of an IP geolocation lookup
/// </summary>
public record IpLocationResult
{
    /// <summary>
    /// Gets the IP address that was looked up
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Gets the country name
    /// </summary>
    public string? CountryName { get; init; }

    /// <summary>
    /// Gets the ISO country code
    /// </summary>
    public string? CountryCode { get; init; }

    /// <summary>
    /// Gets the city name
    /// </summary>
    public string? CityName { get; init; }

    /// <summary>
    /// Gets the subdivision (state/province) name
    /// </summary>
    public string? SubdivisionName { get; init; }

    /// <summary>
    /// Gets the postal code
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets the latitude
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Gets the longitude
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Gets the timezone
    /// </summary>
    public string? TimeZone { get; init; }

    /// <summary>
    /// Gets the ISP/organization name
    /// </summary>
    public string? Isp { get; init; }

    /// <summary>
    /// Gets any error message if the lookup failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the lookup was successful
    /// </summary>
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(CountryCode);

    /// <summary>
    /// Gets the timestamp of the lookup
    /// </summary>
    public DateTime LookedUpAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a result indicating the location was not found for the IP
    /// </summary>
    public static IpLocationResult NotFound(string ip, string? error = null) =>
        new() { IpAddress = ip, ErrorMessage = error ?? "Location not found" };

    /// <summary>
    /// Creates an error result for failed geolocation lookups
    /// </summary>
    public static IpLocationResult Error(string ip, string error) =>
        new() { IpAddress = ip, ErrorMessage = error };
}

/// <summary>
/// IP address types
/// </summary>
public enum IpAddressType
{
    /// <summary>
    /// IPv4 address
    /// </summary>
    IPv4,

    /// <summary>
    /// IPv6 address
    /// </summary>
    IPv6,

    /// <summary>
    /// Unknown address type
    /// </summary>
    Unknown
}
