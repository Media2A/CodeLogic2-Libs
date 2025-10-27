using CL.SocialConnect.Models;
using CL.SocialConnect.Models.Steam;
using CodeLogic.Abstractions;
using System.Net;

namespace CL.SocialConnect.Services.Steam;

/// <summary>
/// Service for Steam OpenID authentication
/// </summary>
public class SteamAuthenticationService
{
    private readonly string _openIdBaseUrl;
    private readonly string _returnUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the SteamAuthenticationService
    /// </summary>
    public SteamAuthenticationService(
        string openIdBaseUrl,
        string returnUrl,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(openIdBaseUrl))
            throw new ArgumentException("OpenID base URL cannot be empty", nameof(openIdBaseUrl));

        if (string.IsNullOrWhiteSpace(returnUrl))
            throw new ArgumentException("Return URL cannot be empty", nameof(returnUrl));

        _openIdBaseUrl = openIdBaseUrl.TrimEnd('/');
        _returnUrl = returnUrl;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create HTTP client with proper handler
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CL.SocialConnect/2.0");
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
    }

    /// <summary>
    /// Generates a Steam login URL for redirecting users to Steam authentication
    /// </summary>
    public async Task<SocialResult<string>> GenerateLoginUrlAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate return URL
            if (!Uri.TryCreate(_returnUrl, UriKind.Absolute, out var returnUri))
                return SocialResult<string>.Failure(
                    SocialError.InvalidCredentials,
                    "Invalid return URL format");

            if (returnUri.Scheme != Uri.UriSchemeHttps && returnUri.Scheme != Uri.UriSchemeHttp)
                return SocialResult<string>.Failure(
                    SocialError.InvalidCredentials,
                    "Return URL must use HTTP or HTTPS");

            var realm = returnUri.GetLeftPart(UriPartial.Authority);

            var loginUrl = $"{_openIdBaseUrl}/login?openid.ns=http://specs.openid.net/auth/2.0" +
                          $"&openid.mode=checkid_setup" +
                          $"&openid.return_to={Uri.EscapeDataString(_returnUrl)}" +
                          $"&openid.realm={Uri.EscapeDataString(realm)}" +
                          $"&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select" +
                          $"&openid.identity=http://specs.openid.net/auth/2.0/identifier_select";

            _logger.Debug("Generated Steam login URL");
            return SocialResult<string>.Success(loginUrl);
        }
        catch (Exception ex)
        {
            _logger.Error("Error generating Steam login URL", ex);
            return SocialResult<string>.Failure(SocialError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Verifies the authentication response from Steam
    /// </summary>
    public async Task<SocialResult<SteamAuthResult>> VerifyAuthenticationAsync(
        string responseUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate response URL
            if (!Uri.TryCreate(responseUrl, UriKind.Absolute, out var uri))
                return SocialResult<SteamAuthResult>.Failure(
                    SocialError.InvalidOAuthCode,
                    "Invalid response URL format");

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return SocialResult<SteamAuthResult>.Failure(
                    SocialError.InvalidOAuthCode,
                    "Response URL must use HTTP or HTTPS");

            // Parse query parameters
            var queryParams = ParseQueryString(uri.Query);

            // Validate required parameters
            var requiredKeys = new[]
            {
                "openid.return_to",
                "openid.assoc_handle",
                "openid.signed",
                "openid.sig",
                "openid.claimed_id",
                "openid.identity",
                "openid.op_endpoint",
                "openid.response_nonce"
            };

            foreach (var key in requiredKeys)
            {
                if (!queryParams.ContainsKey(key) || string.IsNullOrWhiteSpace(queryParams[key]))
                {
                    _logger.Warning($"Missing required OpenID parameter: {key}");
                    return SocialResult<SteamAuthResult>.Failure(
                        SocialError.InvalidOAuthCode,
                        $"Missing required parameter: {key}");
                }
            }

            // Prepare verification request
            var verificationParams = new Dictionary<string, string>();
            foreach (var key in requiredKeys)
            {
                verificationParams[key] = queryParams[key];
            }
            verificationParams["openid.ns"] = "http://specs.openid.net/auth/2.0";
            verificationParams["openid.mode"] = "check_authentication";

            using var content = new FormUrlEncodedContent(verificationParams);
            using var response = await _httpClient.PostAsync(
                $"{_openIdBaseUrl}/login",
                content,
                cancellationToken).ConfigureAwait(false);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Check if authentication was successful
            if (!responseContent.Contains("is_valid:true"))
            {
                _logger.Warning("Steam authentication verification failed");
                return SocialResult<SteamAuthResult>.Failure(
                    SocialError.AuthenticationFailed,
                    "Steam authentication verification failed");
            }

            // Extract Steam ID from claimed_id
            var claimedId = queryParams["openid.claimed_id"];
            var steamId = ExtractSteamId(claimedId);

            if (string.IsNullOrWhiteSpace(steamId))
            {
                _logger.Warning("Failed to extract Steam ID from response");
                return SocialResult<SteamAuthResult>.Failure(
                    SocialError.AuthenticationFailed,
                    "Failed to extract Steam ID");
            }

            _logger.Info($"Steam authentication successful for Steam ID: {steamId}");
            var authResult = SteamAuthResult.Success(steamId);
            return SocialResult<SteamAuthResult>.Success(authResult);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Steam authentication was cancelled");
            return SocialResult<SteamAuthResult>.Failure(SocialError.RequestTimeout, "Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Network error during Steam authentication", ex);
            return SocialResult<SteamAuthResult>.Failure(SocialError.NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error("Error verifying Steam authentication", ex);
            return SocialResult<SteamAuthResult>.Failure(SocialError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Validates if a Steam ID 64-bit is valid
    /// </summary>
    public static bool IsValidSteamId64(string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return false;

        if (!ulong.TryParse(steamId, out var id))
            return false;

        // Steam ID 64 should be > 76561197960265728 (base) and < 76561202255233023
        const ulong minSteamId = 76561197960265728;
        const ulong maxSteamId = 76561202255233023;

        return id >= minSteamId && id <= maxSteamId;
    }

    /// <summary>
    /// Validates if a Steam ID 32-bit is valid
    /// </summary>
    public static bool IsValidSteamId32(string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return false;

        if (!uint.TryParse(steamId, out var id))
            return false;

        // Steam ID 32 should be within valid range
        return id >= 0 && id <= uint.MaxValue;
    }

    /// <summary>
    /// Extracts the Steam ID 64-bit from the OpenID claimed_id URL
    /// </summary>
    private static string? ExtractSteamId(string claimedId)
    {
        if (string.IsNullOrWhiteSpace(claimedId))
            return null;

        if (!Uri.TryCreate(claimedId, UriKind.Absolute, out var uri))
            return null;

        // Check if it's a Steam community URL
        if (!uri.Host.EndsWith("steamcommunity.com", StringComparison.OrdinalIgnoreCase))
            return null;

        // Extract the last segment (Steam ID)
        var steamId = uri.Segments.LastOrDefault()?.Trim('/');

        // Validate that it's all digits
        if (string.IsNullOrWhiteSpace(steamId) || !steamId.All(char.IsDigit))
            return null;

        return steamId;
    }

    /// <summary>
    /// Parses a query string into a dictionary
    /// </summary>
    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(queryString))
            return result;

        // Remove leading '?'
        if (queryString.StartsWith("?"))
            queryString = queryString.Substring(1);

        var pairs = queryString.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Disposes the HTTP client
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
