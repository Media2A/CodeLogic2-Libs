using CL.SocialConnect.Models;
using CL.SocialConnect.Models.Steam;
using CodeLogic.Abstractions;
using System.Text.Json;

namespace CL.SocialConnect.Services.Steam;

/// <summary>
/// Service for retrieving Steam player profile data
/// </summary>
public class SteamProfileService
{
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, (SteamPlayer data, DateTime cached)> _playerCache = new();
    private readonly Dictionary<string, (SteamPlayerBans data, DateTime cached)> _bansCache = new();
    private readonly int _cacheSeconds;
    private readonly bool _enableCaching;

    /// <summary>
    /// Initializes a new instance of the SteamProfileService
    /// </summary>
    public SteamProfileService(
        string apiKey,
        string apiBaseUrl,
        int cacheSeconds = 300,
        bool enableCaching = true,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Steam API key cannot be empty", nameof(apiKey));

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _logger = logger ?? new NullLogger();
        _cacheSeconds = cacheSeconds;
        _enableCaching = enableCaching;
    }

    /// <summary>
    /// Gets a player's profile summary
    /// </summary>
    public async Task<SocialResult<SteamPlayer>> GetPlayerSummaryAsync(
        string steamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(steamId))
                return SocialResult<SteamPlayer>.Failure(SocialError.UserNotFound, "Steam ID cannot be empty");

            // Check cache
            if (_enableCaching && _playerCache.TryGetValue(steamId, out var cached))
            {
                if ((DateTime.UtcNow - cached.cached).TotalSeconds < _cacheSeconds)
                {
                    _logger.Debug($"Player summary for {steamId} retrieved from cache");
                    return SocialResult<SteamPlayer>.Success(cached.data);
                }
                _playerCache.Remove(steamId);
            }

            var url = $"{_apiBaseUrl}/ISteamUser/GetPlayerSummaries/v0002/?" +
                     $"key={Uri.EscapeDataString(_apiKey)}&steamids={Uri.EscapeDataString(steamId)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return SocialResult<SteamPlayer>.Failure(SocialError.NetworkError, $"HTTP {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var responseElement))
                return SocialResult<SteamPlayer>.Failure(SocialError.Unknown, "Invalid response format");

            if (!responseElement.TryGetProperty("players", out var playersElement) || playersElement.GetArrayLength() == 0)
                return SocialResult<SteamPlayer>.Failure(SocialError.UserNotFound, "Player not found");

            var player = DeserializePlayer(playersElement[0]);
            if (_enableCaching)
                _playerCache[steamId] = (player, DateTime.UtcNow);

            _logger.Debug($"Retrieved player summary for {steamId}");
            return SocialResult<SteamPlayer>.Success(player);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Steam API request was cancelled");
            return SocialResult<SteamPlayer>.Failure(SocialError.RequestTimeout, "Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Network error retrieving player summary", ex);
            return SocialResult<SteamPlayer>.Failure(SocialError.NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving player summary for {steamId}", ex);
            return SocialResult<SteamPlayer>.Failure(SocialError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Gets ban information for a player
    /// </summary>
    public async Task<SocialResult<SteamPlayerBans>> GetPlayerBansAsync(
        string steamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(steamId))
                return SocialResult<SteamPlayerBans>.Failure(SocialError.UserNotFound, "Steam ID cannot be empty");

            // Check cache
            if (_enableCaching && _bansCache.TryGetValue(steamId, out var cached))
            {
                if ((DateTime.UtcNow - cached.cached).TotalSeconds < _cacheSeconds)
                {
                    _logger.Debug($"Ban info for {steamId} retrieved from cache");
                    return SocialResult<SteamPlayerBans>.Success(cached.data);
                }
                _bansCache.Remove(steamId);
            }

            var url = $"{_apiBaseUrl}/ISteamUser/GetPlayerBans/v1/?" +
                     $"key={Uri.EscapeDataString(_apiKey)}&steamids={Uri.EscapeDataString(steamId)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return SocialResult<SteamPlayerBans>.Failure(SocialError.NetworkError, $"HTTP {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("players", out var playersElement) || playersElement.GetArrayLength() == 0)
                return SocialResult<SteamPlayerBans>.Failure(SocialError.UserNotFound, "Player not found");

            var bans = DeserializePlayerBans(playersElement[0], steamId);
            if (_enableCaching)
                _bansCache[steamId] = (bans, DateTime.UtcNow);

            _logger.Debug($"Retrieved ban info for {steamId}");
            return SocialResult<SteamPlayerBans>.Success(bans);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Steam API request was cancelled");
            return SocialResult<SteamPlayerBans>.Failure(SocialError.RequestTimeout, "Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Network error retrieving player bans", ex);
            return SocialResult<SteamPlayerBans>.Failure(SocialError.NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving bans for {steamId}", ex);
            return SocialResult<SteamPlayerBans>.Failure(SocialError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Clears the player cache
    /// </summary>
    public void ClearCache()
    {
        _playerCache.Clear();
        _bansCache.Clear();
        _logger.Debug("Steam profile cache cleared");
    }

    private static SteamPlayer DeserializePlayer(JsonElement element)
    {
        return new SteamPlayer
        {
            SteamId = element.GetProperty("steamid").GetString() ?? string.Empty,
            PersonaName = element.GetProperty("personaname").GetString() ?? string.Empty,
            ProfileUrl = element.GetProperty("profileurl").GetString() ?? string.Empty,
            Avatar = element.GetProperty("avatar").GetString() ?? string.Empty,
            AvatarMedium = element.GetProperty("avatarmedium").GetString() ?? string.Empty,
            AvatarFull = element.GetProperty("avatarfull").GetString() ?? string.Empty,
            PersonaState = element.GetProperty("personastate").GetInt32(),
            LastLogoff = element.TryGetProperty("lastlogoff", out var lf) ? UnixTimeStampToDateTime(lf.GetInt64()) : null,
            CityName = element.TryGetProperty("loccityid", out _) ? element.GetProperty("loccityid").GetString() : null,
            CountryCode = element.TryGetProperty("loccountrycode", out var cc) ? cc.GetString() : null,
            SteamLevel = element.TryGetProperty("communityvisibilitystate", out _) ? (int?)null : null,
            VisibilityState = element.TryGetProperty("communityvisibilitystate", out var vis) ? vis.GetInt32() : 1,
            RealName = element.TryGetProperty("realname", out var rn) ? rn.GetString() : null,
            TimeCreated = element.TryGetProperty("timecreated", out var tc) ? UnixTimeStampToDateTime(tc.GetInt64()) : null
        };
    }

    private static SteamPlayerBans DeserializePlayerBans(JsonElement element, string steamId)
    {
        return new SteamPlayerBans
        {
            SteamId = steamId,
            CommunityBanned = element.TryGetProperty("CommunityBanned", out var cb) && cb.GetBoolean(),
            VacBanned = element.TryGetProperty("VACBanned", out var vb) && vb.GetBoolean(),
            DaysSinceLastBan = element.TryGetProperty("DaysSinceLastBan", out var dsb) ? dsb.GetInt32() : -1,
            NumberOfGameBans = element.TryGetProperty("NumberOfGameBans", out var ngb) ? ngb.GetInt32() : 0,
            NumberOfVacBans = element.TryGetProperty("NumberOfVACBans", out var nvb) ? nvb.GetInt32() : 0,
            EconomyBan = element.TryGetProperty("EconomyBan", out var eb) ? eb.GetString() : "none"
        };
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}

// Null logger for when no logger is provided
internal class NullLogger : ILogger
{
    public void Log(LogLevel level, string message, Exception? exception = null, params object[] args) { }
    public void Trace(string message, params object[] args) { }
    public void Debug(string message, params object[] args) { }
    public void Info(string message, params object[] args) { }
    public void Warning(string message, params object[] args) { }
    public void Error(string message, Exception? exception = null, params object[] args) { }
    public void Fatal(string message, Exception? exception = null, params object[] args) { }
}
