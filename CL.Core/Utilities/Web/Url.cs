using Microsoft.AspNetCore.Http.Extensions;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {

        // Url
        public static string GetPath()
        {
            var Domain = CLU_Web.HC().Request.Path.Value.ToString();
            return Domain;
        }

        public static string GetDisplayUrl()
        {
            var QueryString = CLU_Web.HC().Request.GetDisplayUrl();
            return QueryString;
        }

        public static string GetFullEncodedUrl()
        {
            var QueryString = CLU_Web.HC().Request.GetEncodedUrl();
            return QueryString;
        }

        public static string GetRawUrl()
        {
            var web = CLU_Web.HC();
            return $"{web.Request.Scheme}://{web.Request.Host}{web.Request.Path}{web.Request.QueryString}";
        }
        public static string GetUrlDomain()
        {
            var web = CLU_Web.HC();
            return $"{web.Request.Scheme}://{web.Request.Host}/";
        }
        // Querystring

        public static string GetFullQueryString()
        {
            var QueryString = CLU_Web.HC().Request.QueryString.ToString();
            return QueryString;
        }

        // Url path split
        // Url path split
        public static string SplitUrlString(string rawUrl, int intSplitID)
        {
            // Remove query string if present
            int queryIndex = rawUrl.IndexOf('?');
            if (queryIndex != -1)
            {
                rawUrl = rawUrl.Substring(0, queryIndex);
            }

            // Check if the URL contains the protocol (http:// or https://)
            int protocolIndex = rawUrl.IndexOf("://");
            if (protocolIndex != -1)
            {
                int hostStartIndex = protocolIndex + 3;
                int pathStartIndex = rawUrl.IndexOf('/', hostStartIndex);

                // Check if there's a colon (:) after the protocol part indicating a port number
                int portIndex = rawUrl.IndexOf(':', hostStartIndex);
                if (portIndex != -1 && portIndex < pathStartIndex)
                {
                    hostStartIndex = portIndex + 1;
                }

                // If there is no slash after the hostname, set pathStartIndex to the end of the URL
                if (pathStartIndex == -1)
                {
                    pathStartIndex = rawUrl.Length;
                }

                // Extract the path part
                string path = rawUrl.Substring(pathStartIndex + 1);

                // Split the path by '/' and remove empty segments
                string[] segments = path.Split('/').Where(segment => !string.IsNullOrEmpty(segment)).ToArray();

                if (intSplitID >= 0 && intSplitID < segments.Length)
                {
                    return segments[intSplitID];
                }
            }

            // Return "root" when intSplitID is out of range or there's no matching path
            return "root";
        }

        public static List<string> SplitUrlStringToList(string rawUrl)
        {
            List<string> paths = new List<string>();

            // Remove query string if present
            int queryIndex = rawUrl.IndexOf('?');
            if (queryIndex != -1)
            {
                rawUrl = rawUrl.Substring(0, queryIndex);
            }

            // Check if the URL contains the protocol (http:// or https://)
            int protocolIndex = rawUrl.IndexOf("://");
            if (protocolIndex != -1)
            {
                int hostStartIndex = protocolIndex + 3;
                int pathStartIndex = rawUrl.IndexOf('/', hostStartIndex);

                // Check if there's a colon (:) after the protocol part indicating a port number
                int portIndex = rawUrl.IndexOf(':', hostStartIndex);
                if (portIndex != -1 && portIndex < pathStartIndex)
                {
                    hostStartIndex = portIndex + 1;
                }

                // If there is no slash after the hostname, set pathStartIndex to the end of the URL
                if (pathStartIndex == -1)
                {
                    pathStartIndex = rawUrl.Length;
                }

                // Extract the path part
                string path = rawUrl.Substring(pathStartIndex + 1);

                // Split the path by '/' and remove empty segments
                string[] segments = path.Split('/').Where(segment => !string.IsNullOrEmpty(segment)).ToArray();

                paths.AddRange(segments);
            }

            // If no paths found, add "root"
            if (paths.Count == 0)
            {
                paths.Add("root");
            }

            return paths;
        }
    }
}