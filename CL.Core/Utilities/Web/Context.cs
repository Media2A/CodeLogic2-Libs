using Microsoft.AspNetCore.Http;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        private static IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Configures the IHttpContextAccessor. This should be called once during application startup.
        /// </summary>
        /// <param name="httpContextAccessor">The IHttpContextAccessor instance.</param>
        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the current HTTP context.
        /// </summary>
        /// <returns>The current <see cref="HttpContext"/>.</returns>
        public static HttpContext HC()
        {
            return _httpContextAccessor?.HttpContext;
        }

        /// <summary>
        /// Sets data in the current HTTP context's Items collection.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="data">The data to set.</param>
        public static void SetDataContext(string name, object data)
        {
            var context = HC();
            if (context != null)
            {
                context.Items[name] = data;
            }
        }

        /// <summary>
        /// Gets data from the current HTTP context's Items collection.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The data associated with the key, or null if not found.</returns>
        public static object GetDataContext(string name)
        {
            var context = HC();
            if (context != null && context.Items.ContainsKey(name))
            {
                return context.Items[name];
            }
            return null;
        }

    }
}
