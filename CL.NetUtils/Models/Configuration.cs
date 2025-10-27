namespace CL.NetUtils.Models;

/// <summary>
/// Configuration for DNS blacklist (DNSBL) checking
/// </summary>
public class DnsblConfiguration
{
    /// <summary>
    /// Gets or sets whether DNSBL checking is enabled globally
    /// </summary>
    public bool EnableDnsblCheck { get; set; } = true;

    /// <summary>
    /// Gets or sets IPv4 DNSBL configuration
    /// </summary>
    public Ipv4DnsblConfiguration Ipv4Config { get; set; } = new();

    /// <summary>
    /// Gets or sets IPv6 DNSBL configuration
    /// </summary>
    public Ipv6DnsblConfiguration Ipv6Config { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable detailed logging of DNSBL results
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// IPv4-specific DNSBL configuration
/// </summary>
public class Ipv4DnsblConfiguration
{
    /// <summary>
    /// Gets or sets whether IPv4 DNSBL checking is enabled
    /// </summary>
    public bool EnableIpv4Check { get; set; } = true;

    /// <summary>
    /// Gets or sets the DNSBL services to check
    /// </summary>
    public List<string> DnsblServices { get; set; } = new()
    {
        "zen.spamhaus.org",
        "dnsbl.sorbs.net"
    };

    /// <summary>
    /// Gets or sets the DNS lookup timeout in milliseconds
    /// </summary>
    public int DnsLookupTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets fallback DNSBL services if primary ones fail
    /// </summary>
    public List<string> FallbackDnsblServices { get; set; } = new()
    {
        "b.barracudacentral.org"
    };
}

/// <summary>
/// IPv6-specific DNSBL configuration
/// </summary>
public class Ipv6DnsblConfiguration
{
    /// <summary>
    /// Gets or sets whether IPv6 DNSBL checking is enabled
    /// </summary>
    public bool EnableIpv6Check { get; set; } = true;

    /// <summary>
    /// Gets or sets the DNSBL services to check for IPv6
    /// </summary>
    public List<string> DnsblServices { get; set; } = new()
    {
        "zen.spamhaus.org",
        "dnsbl6.sorbs.net"
    };

    /// <summary>
    /// Gets or sets the DNS lookup timeout in milliseconds
    /// </summary>
    public int DnsLookupTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets fallback DNSBL services for IPv6
    /// </summary>
    public List<string> FallbackDnsblServices { get; set; } = new()
    {
        "v6.b.barracudacentral.org"
    };
}

/// <summary>
/// Configuration for IP geolocation using MaxMind database
/// </summary>
public class Ip2LocationConfiguration
{
    /// <summary>
    /// Gets or sets the MaxMind account ID
    /// </summary>
    public int AccountID { get; set; } = 0;

    /// <summary>
    /// Gets or sets the MaxMind license key
    /// </summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory path for storing the database
    /// </summary>
    public string DatabaseDirectory { get; set; } = "data/maxmind/";

    /// <summary>
    /// Gets or sets the download URL for the GeoLite2 database
    /// </summary>
    public string DownloadUrl { get; set; } = "https://download.maxmind.com/geoip/databases/GeoLite2-City/download?suffix=tar.gz";

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the database type (e.g., GeoLite2-City)
    /// </summary>
    public string DatabaseType { get; set; } = "GeoLite2-City";
}
