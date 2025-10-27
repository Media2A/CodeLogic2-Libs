using CL.Core.Utilities;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CL.Core.Utilities
{
    /// <summary>
    /// Web-related utility functions for accessing request headers, client information, and locale-aware settings.
    /// </summary>
    public partial class CLU_Web
    {
        /// <summary>
        /// Gets the primary locale string from the browser's Accept-Language header.
        /// </summary>
        /// <returns>A locale string (e.g., "en-US") or "en-US" if none found.</returns>
        public static string GetPrimaryLocale()
        {
            string[] userLanguages = CLU_Web.HC().Request.GetTypedHeaders()
                       .AcceptLanguage
                       ?.OrderByDescending(x => x.Quality ?? 1)
                       .Select(x => x.Value.ToString())
                       .ToArray() ?? Array.Empty<string>();

            if (userLanguages != null && userLanguages.Length > 0)
            {
                return userLanguages[0];
            }

            return "en-US";
        }

        /// <summary>
        /// Gets the current local date and time based on the primary locale's time zone.
        /// </summary>
        /// <returns>Localized <see cref="DateTime"/> object.</returns>
        public static DateTime GetLocaleDateTime()
        {
            // Returns UTC time - implement custom timezone mapping if needed
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> for the primary locale.
        /// </summary>
        /// <returns>CultureInfo object representing the primary locale.</returns>
        public static CultureInfo GetCultureInfo()
        {
            string primaryLocale = GetPrimaryLocale();
            return new CultureInfo(primaryLocale);
        }

        /// <summary>
        /// Gets the <see cref="TimeZoneInfo"/> object for the primary locale's time zone.
        /// </summary>
        /// <returns>TimeZoneInfo object or UTC if not found.</returns>
        public static TimeZoneInfo GetTimeZoneInfo()
        {
            // Returns UTC timezone - implement custom timezone mapping if needed
            return TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Gets the direct remote IP address of the client.
        /// </summary>
        /// <returns>IP address string.</returns>
        public static string GetClientIP()
        {
            var remoteIpAddress = CLU_Web.HC().Connection.RemoteIpAddress.ToString();
            return remoteIpAddress;
        }

        /// <summary>
        /// Gets the IP address from the X-Forwarded-For header (useful behind proxies).
        /// </summary>
        /// <returns>Forwarded IP address string.</returns>
        public static string GetClientXforwardIP()
        {
            var xForwardIP = CLU_Web.HC().Request.Headers["X-Forwarded-For"].ToString();
            return xForwardIP;
        }

        /// <summary>
        /// Checks if the provided IP address is within a local/private IP range.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the IP is local/private; otherwise, false.</returns>
        public static bool IsLocalIPRange(string ipAddress)
        {
            // Quickly fail on null, empty, or weird IPs
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;

            var split = ipAddress.Split('.');
            if (split.Length != 4) return false; // Only support IPv4 here

            int[] ipParts = new int[4];
            for (int i = 0; i < 4; i++)
                if (!int.TryParse(split[i], out ipParts[i])) return false;

            if (ipParts[0] == 10 ||
                (ipParts[0] == 192 && ipParts[1] == 168) ||
                (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to detect if the current request is made by a known web crawler or bot.
        /// </summary>
        /// <returns>True if the request is likely from a crawler; otherwise, false.</returns>
        public static bool IsWebCrawler()
        {
            bool crawlerCheck = Regex.IsMatch(CLU_Web.HC().Request.Headers["User-Agent"].ToString(),
                @"bot|crawler|baiduspider|80legs|ia_archiver|voyager|curl|wget|yahoo! slurp|mediapartners-google",
                RegexOptions.IgnoreCase);

            return false; // NOTE: currently always false; update logic if needed
        }

        /// <summary>
        /// Gets the base language code from the Accept-Language header.
        /// </summary>
        /// <returns>A 2-character ISO language code, or "invalid" if not found.</returns>
        public static string GetClientLanguage()
        {
            var clientLanguage = CLU_Web.HC().Request.Headers["Accept-Language"].ToString().Split(";").FirstOrDefault()?.Split(",").FirstOrDefault();

            if (clientLanguage != null)
            {
                return clientLanguage.Substring(0, 2);
            }
            else
            {
                return "invalid";
            }
        }

        /// <summary>
        /// Determines the effective client IP address. If the IP is local, attempts to find a public IP in the X-Forwarded-For header.
        /// </summary>
        /// <returns>The most accurate public-facing IP address.</returns>
        public static string GetEffectiveClientIP()
        {
            var xForwardedFor = CLU_Web.HC().Request.Headers["X-Forwarded-For"].ToString();
            var remoteIp = CLU_Web.HC().Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            // Split, trim, and remove empty entries
            var forwardedIps = xForwardedFor?
                .Split(',')
                .Select(ip => ip.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToArray() ?? Array.Empty<string>();

            // Use first valid forwarded IP, or remote IP if none
            var clientIp = forwardedIps.FirstOrDefault() ?? remoteIp;

            // Try to find a public IP if first is private
            if (IsLocalIPRange(clientIp))
            {
                var publicIp = forwardedIps.FirstOrDefault(ip => !IsLocalIPRange(ip));
                if (!string.IsNullOrEmpty(publicIp))
                {
                    return publicIp;
                }
                // fallback to remoteIp if all are local
                return remoteIp;
            }

            return clientIp;
        }

    }
}
