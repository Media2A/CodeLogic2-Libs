using System;
using Microsoft.AspNetCore.Http;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        /// <summary>
        /// Represents the parameters for configuring client cookies.
        /// </summary>
        public class ClientCookieParameters
        {
            /// <summary>
            /// Gets or sets a value indicating whether the cookie is accessible only through HTTP requests.
            /// </summary>
            public bool HttpOnly { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether the cookie is sent only over HTTPS.
            /// </summary>
            public bool Secure { get; set; } = true;

            /// <summary>
            /// Gets or sets the SameSite attribute of the cookie to control its behavior with cross-site requests.
            /// </summary>
            public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

            /// <summary>
            /// Gets or sets the expiration date and time of the cookie.
            /// </summary>
            public DateTimeOffset? Expires { get; set; } = DateTimeOffset.UtcNow.AddDays(30);

            /// <summary>
            /// Gets or sets the domain for which the cookie is valid.
            /// </summary>
            public string Domain { get; set; } = null;

            /// <summary>
            /// Gets or sets the path for which the cookie is valid.
            /// </summary>
            public string Path { get; set; } = "/";
        }

        /// <summary>
        /// Sets a client cookie with the specified key and value, using the provided cookie parameters.
        /// </summary>
        /// <param name="key">The key of the cookie.</param>
        /// <param name="value">The value of the cookie.</param>
        /// <param name="cookieParameters">The parameters for configuring the cookie. If null, default parameters will be used.</param>
        public static void SetClientCookie(string key, string value, ClientCookieParameters cookieParameters = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cookie key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentException("Cookie value cannot be null", nameof(value));
            }

            if (cookieParameters == null)
            {
                cookieParameters = new ClientCookieParameters();
            }

            var context = CLU_Web.HC();
            if (context == null)
            {
                throw new InvalidOperationException("HttpContext cannot be null.");
            }

            try
            {
                context.Response.Cookies.Append(key, value, new CookieOptions
                {
                    HttpOnly = cookieParameters.HttpOnly,
                    Secure = cookieParameters.Secure,
                    SameSite = cookieParameters.SameSite,
                    Expires = cookieParameters.Expires,
                    Domain = cookieParameters.Domain,
                    Path = cookieParameters.Path
                });
            }
            catch (Exception ex)
            {
                // Log the exception and rethrow or handle it as needed
                throw new InvalidOperationException("An error occurred while setting the cookie.", ex);
            }
        }

        /// <summary>
        /// Gets the value of a client cookie with the specified key.
        /// </summary>
        /// <param name="key">The key of the cookie.</param>
        /// <returns>The value of the cookie if found; otherwise, null.</returns>
        public static string GetClientCookie(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cookie key cannot be null or empty", nameof(key));
            }

            var context = CLU_Web.HC();
            if (context == null)
            {
                throw new InvalidOperationException("HttpContext cannot be null.");
            }

            context.Request.Cookies.TryGetValue(key, out var value);
            return value;
        }

    }
}
