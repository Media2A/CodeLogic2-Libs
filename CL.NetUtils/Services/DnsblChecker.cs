using System.Net;
using System.Net.Sockets;
using CL.NetUtils.Models;
using CodeLogic.Abstractions;

namespace CL.NetUtils.Services;

/// <summary>
/// Provides DNS blacklist checking for IP addresses
/// </summary>
public class DnsblChecker
{
    private readonly DnsblConfiguration _config;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the DnsblChecker class
    /// </summary>
    public DnsblChecker(DnsblConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if an IP address is blacklisted on any configured DNSBL service
    /// </summary>
    public async Task<DnsblCheckResult> CheckIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                _logger.Warning($"Invalid IP address: {ipAddress}");
                return DnsblCheckResult.Error(ipAddress, IpAddressType.Unknown, "Invalid IP address format");
            }

            // Check if IP is local or private
            if (IsLocalOrPrivateIp(ip))
            {
                _logger.Debug($"Skipping DNSBL check for local/private IP: {ipAddress}");
                return DnsblCheckResult.NotBlacklisted(ipAddress, GetAddressType(ip));
            }

            // Check DNSBL based on IP type
            if (ip.AddressFamily == AddressFamily.InterNetwork && _config.Ipv4Config.EnableIpv4Check)
            {
                return await CheckIpv4Async(ipAddress, _config.Ipv4Config, cancellationToken);
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6 && _config.Ipv6Config.EnableIpv6Check)
            {
                return await CheckIpv6Async(ipAddress, _config.Ipv6Config, cancellationToken);
            }

            _logger.Debug($"DNSBL check disabled for {ipAddress}");
            return DnsblCheckResult.NotBlacklisted(ipAddress, GetAddressType(ip));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking DNSBL for {ipAddress}", ex);
            return DnsblCheckResult.Error(ipAddress, IpAddressType.Unknown, ex.Message);
        }
    }

    private async Task<DnsblCheckResult> CheckIpv4Async(string ipAddress, Ipv4DnsblConfiguration config, CancellationToken cancellationToken)
    {
        var reversedIp = string.Join(".", ipAddress.Split('.').Reverse());

        // Try primary services
        foreach (var service in config.DnsblServices)
        {
            var result = await CheckServiceAsync($"{reversedIp}.{service}", ipAddress, service, config.DnsLookupTimeoutMs, cancellationToken);
            if (result.IsBlacklisted)
            {
                _logger.Warning($"IPv4 {ipAddress} is blacklisted by {service}");
                return result;
            }
        }

        // Try fallback services if enabled
        foreach (var service in config.FallbackDnsblServices)
        {
            var result = await CheckServiceAsync($"{reversedIp}.{service}", ipAddress, service, config.DnsLookupTimeoutMs, cancellationToken);
            if (result.IsBlacklisted)
            {
                _logger.Warning($"IPv4 {ipAddress} is blacklisted by fallback service {service}");
                return result;
            }
        }

        _logger.Debug($"IPv4 {ipAddress} is not blacklisted");
        return DnsblCheckResult.NotBlacklisted(ipAddress, IpAddressType.IPv4);
    }

    private async Task<DnsblCheckResult> CheckIpv6Async(string ipAddress, Ipv6DnsblConfiguration config, CancellationToken cancellationToken)
    {
        var reversedIpv6 = ReverseIpv6(ipAddress);

        // Try primary services
        foreach (var service in config.DnsblServices)
        {
            var result = await CheckServiceAsync($"{reversedIpv6}.{service}", ipAddress, service, config.DnsLookupTimeoutMs, cancellationToken);
            if (result.IsBlacklisted)
            {
                _logger.Warning($"IPv6 {ipAddress} is blacklisted by {service}");
                return result;
            }
        }

        // Try fallback services if enabled
        foreach (var service in config.FallbackDnsblServices)
        {
            var result = await CheckServiceAsync($"{reversedIpv6}.{service}", ipAddress, service, config.DnsLookupTimeoutMs, cancellationToken);
            if (result.IsBlacklisted)
            {
                _logger.Warning($"IPv6 {ipAddress} is blacklisted by fallback service {service}");
                return result;
            }
        }

        _logger.Debug($"IPv6 {ipAddress} is not blacklisted");
        return DnsblCheckResult.NotBlacklisted(ipAddress, IpAddressType.IPv6);
    }

    private async Task<DnsblCheckResult> CheckServiceAsync(string lookup, string originalIp, string service, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var result = await Task.Run(() => Dns.GetHostAddresses(lookup), linkedCts.Token);

            if (result.Length > 0)
            {
                return DnsblCheckResult.Blacklisted(originalIp, GetAddressType(IPAddress.Parse(originalIp)), service);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug($"DNS lookup timeout for {lookup}");
        }
        catch (Exception ex)
        {
            _logger.Debug($"DNS lookup failed for {lookup}: {ex.Message}");
        }

        return DnsblCheckResult.NotBlacklisted(originalIp, GetAddressType(IPAddress.Parse(originalIp)));
    }

    private bool IsLocalOrPrivateIp(IPAddress ip)
    {
        // Check loopback
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Link-local, site-local, multicast
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                return true;

            // Unique local address fc00::/7
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 0xfc || bytes[0] == 0xfd)
                return true;
        }

        return false;
    }

    private string ReverseIpv6(string ipv6)
    {
        var ip = IPAddress.Parse(ipv6);
        var expanded = ip.ToString().Replace(":", "");
        return string.Join(".", expanded.Reverse());
    }

    private IpAddressType GetAddressType(IPAddress ip)
    {
        return ip.AddressFamily switch
        {
            AddressFamily.InterNetwork => IpAddressType.IPv4,
            AddressFamily.InterNetworkV6 => IpAddressType.IPv6,
            _ => IpAddressType.Unknown
        };
    }
}
