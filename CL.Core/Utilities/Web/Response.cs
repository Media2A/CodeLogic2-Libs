using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CL.Core.Utilities
{
    /// <summary>
    /// Provides utility methods for web response handling.
    /// </summary>
    public partial class CLU_Web
    {
        /// <summary>
        /// Specifies the types of caching available.
        /// </summary>
        public enum CachingType
        {
            DISABLED,
            BROWSER
        }

        /// <summary>
        /// Writes the specified text to the response stream with optional caching.
        /// </summary>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="cachingType">The type of caching to use.</param>
        /// <param name="cachingSecs">The duration in seconds for browser caching.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ResponseText(string text, CachingType cachingType = CachingType.DISABLED, int cachingSecs = 3600)
        {
            switch (cachingType)
            {
                case CachingType.DISABLED:
                    CLU_Web.SetHttpHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                    CLU_Web.SetHttpHeader("Pragma", "no-cache");
                    CLU_Web.SetHttpHeader("Expires", "0");
                    await CLU_Web.WriteAsync(text);
                    break;

                case CachingType.BROWSER:
                    CLU_Web.SetHttpHeader("Cache-Control", $"public, max-age={cachingSecs}"); // Cache for cachingSecs seconds
                    await CLU_Web.WriteAsync(text);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Writes the specified binary stream to the response stream as a file download.
        /// </summary>
        /// <param name="stream">The binary data to write to the response.</param>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        public static void ResponseBinaryStream(byte[] stream, string fileName)
        {
            // Set response headers to indicate file download
            CLU_Web.SetHttpHeader("Content-Disposition", $"attachment; filename={fileName}");

            // Write the binary stream to the response stream
            CLU_Web.WriteBinaryStreamAsync(stream);
        }

        /// <summary>
        /// Writes the contents of the specified file to the response stream as a file download.
        /// </summary>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        public static void ResponseWithFileAttachment(string fileName)
        {
            // Set response headers to indicate file download
            CLU_Web.SetHttpHeader("Content-Disposition", $"attachment; filename={fileName}");

            // Read the file content
            string filePath = $"{fileName}"; // Update with the actual file path
            string fileContent = File.ReadAllText(filePath); // Read the file content

            CLU_Web.WriteAsync(fileContent); // Write the file content to the response stream
        }

        /// <summary>
        /// Redirects to the given url
        /// </summary>
        /// <param name="url">The name of the url.</param>
        public static void ResponseRedirect(string url)
        {
            var context = CLU_Web.HC(); // Ensure this correctly retrieves HttpContext

            if (context == null)
            {
                throw new InvalidOperationException("HttpContext is not available.");
            }

            context.Response.Redirect(url);
        }

        /// <summary>
        /// Redirects to the given url
        /// </summary>
        /// <param name="url">The name of the url.</param>
        public static async Task ResponseRedirectAsync(string url)
        {
            var context = CLU_Web.HC(); // Ensure this correctly retrieves HttpContext

            if (context == null)
            {
                throw new InvalidOperationException("HttpContext is not available.");
            }

            context.Response.StatusCode = StatusCodes.Status302Found; // or 301 for permanent redirect
            context.Response.Headers["Location"] = url;

            // Optionally, you can write a response body
            await context.Response.WriteAsync($"Redirecting to <a href='{url}'>{url}</a>");

            // Ensure no further processing is done
            await context.Response.Body.FlushAsync();
        }

        // Clean up later...

        public class ContentType
        {
            public enum ContentTypeEnum
            {
                Html,
                Plain,
                Json,
                Xml,
                Javascript,
                OctetStream,
                Pdf,
                Png,
                Gif,
                SvgXml,
                Css,
                Csv,
                Zip,
                UrlEncoded,
                FormData,
                Mpeg,
                Mp4
            }

            private static readonly Dictionary<ContentTypeEnum, string> ContentTypes = new Dictionary<ContentTypeEnum, string>
            {
                { ContentTypeEnum.Html, "text/html" },
                { ContentTypeEnum.Plain, "text/plain" },
                { ContentTypeEnum.Json, "application/json" },
                { ContentTypeEnum.Xml, "application/xml" },
                { ContentTypeEnum.Javascript, "application/javascript" },
                { ContentTypeEnum.OctetStream, "application/octet-stream" },
                { ContentTypeEnum.Pdf, "application/pdf" },
                { ContentTypeEnum.Png, "image/png" },
                { ContentTypeEnum.Gif, "image/gif" },
                { ContentTypeEnum.SvgXml, "image/svg+xml" },
                { ContentTypeEnum.Css, "text/css" },
                { ContentTypeEnum.Csv, "text/csv" },
                { ContentTypeEnum.Zip, "application/zip" },
                { ContentTypeEnum.UrlEncoded, "application/x-www-form-urlencoded" },
                { ContentTypeEnum.FormData, "multipart/form-data" },
                { ContentTypeEnum.Mpeg, "audio/mpeg" },
                { ContentTypeEnum.Mp4, "video/mp4" }
            };

            /// <summary>
            /// Gets the content type string for the specified content type enum value.
            /// </summary>
            /// <param name="contentTypeEnum">The content type enum value.</param>
            /// <returns>The corresponding content type string.</returns>
            public static string GetContentType(ContentTypeEnum contentTypeEnum)
            {
                return ContentTypes[contentTypeEnum];
            }
        }

        /// <summary>
        /// Writes the specified data to the response body as a UTF-8 encoded string.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteAsync(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                HttpContext context = HC();
                if (context != null)
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    await context.Response.Body.WriteAsync(dataBytes, 0, dataBytes.Length);
                }
            }
        }

        /// <summary>
        /// Writes the specified binary data to the response body.
        /// </summary>
        /// <param name="data">The binary data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteBinaryStreamAsync(byte[]? data)
        {
            if (data != null && data.Length > 0)
            {
                HttpContext context = HC();
                if (context != null)
                {
                    await context.Response.Body.WriteAsync(data, 0, data.Length);
                    await context.Response.Body.FlushAsync();
                }
            }
        }

        /// <summary>
        /// Writes a JSON response with the specified data and status code.
        /// </summary>
        /// <param name="data">The data to serialize and send as JSON.</param>
        /// <param name="statusCode">The HTTP status code to set. Defaults to 200 (OK).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteJsonResponseAsync(string data, int statusCode = 200)
        {
            HttpContext context = HC();
            if (context != null)
            {
                await context.Response.WriteAsJsonAsync(data);
            }
        }

        public static async Task WriteJsonSerializeAsync(object data)
        {
            var jsonResult = JsonConvert.SerializeObject(data);
            HttpContext context = HC();
            if (context != null)
            {
                await context.Response.WriteAsJsonAsync(jsonResult);
            }
        }

        /// <summary>
        /// Sends a response with the specified content type and data.
        /// </summary>
        /// <param name="data">The response data to send.</param>
        /// <param name="contentType">The content type of the response.</param>
        /// <param name="statusCode">The HTTP status code to send. Defaults to 200 (OK).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteResponseAsync(string data, ContentType.ContentTypeEnum contentType, int statusCode = 200)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }
            HttpContext context = HC();
            if (context != null)
            {
                // It's default status 200 otherwise it will fail.
                if(statusCode != 200)
                {
                    context.Response.StatusCode = statusCode;
                }
                context.Response.ContentType = ContentType.GetContentType(contentType);

                await context.Response.WriteAsync(data);
                await context.Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// Converts the T model and sends a json response
        /// </summary>
        /// <param name="data">The response data to send.</param>
        /// <param name="contentType">The content type of the response.</param>
        /// <param name="statusCode">The HTTP status code to send. Defaults to 200 (OK).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteResponseModelAsync(object model, ContentType.ContentTypeEnum contentType, int statusCode = 200)
        {
            HttpContext context = HC();
            if (context != null)
            {
                var response = JsonConvert.SerializeObject(model);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = ContentType.GetContentType(contentType);

                await context.Response.WriteAsync(response);
                await context.Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// Minifies the provided HTML by removing unnecessary whitespace while preserving content
        /// within specific tags.
        /// </summary>
        /// <param name="input">The HTML input to minify.</param>
        /// <returns>The minified HTML string.</returns>
        public static string MinifyHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Preserve <pre>, <textarea>, <script>, <style>, and <code> content
            string[] tagsToPreserve = { "pre", "textarea", "script", "style", "code" };

            foreach (var tag in tagsToPreserve)
            {
                input = PreserveTagContent(input, tag);
            }

            // Minify by trimming whitespace between HTML tags
            var parts = input.Split(new[] { '>' }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }
            string minifiedHtml = string.Join(">", parts);

            return minifiedHtml;
        }

        /// <summary> Preserves the content within a specific HTML tag by replacing '>' and '<' to
        /// avoid minifying the content inside. </summary> <param name="input">The HTML input
        /// string.</param> <param name="tag">The tag name to preserve.</param> <returns>The HTML
        /// string with preserved tag content.</returns>
        private static string PreserveTagContent(string input, string tag)
        {
            int startIndex = 0;
            while ((startIndex = input.IndexOf($"<{tag}", startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int endIndex = input.IndexOf($"</{tag}>", startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex == -1) break;

                endIndex += tag.Length + 3; // Length of "</tag>"
                string originalContent = input.Substring(startIndex, endIndex - startIndex);

                string preservedContent = originalContent.Replace(">", "> ")
                                                          .Replace("<", " <");

                input = input.Replace(originalContent, preservedContent);
                startIndex = endIndex;
            }
            return input;
        }
    }
}