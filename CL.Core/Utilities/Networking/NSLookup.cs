using System.Net;

namespace CL.Core.Utilities
{
    public partial class CLU_Networking
    {
        /// <summary>
        /// Performs an NSLookup for the specified domain name and retrieves IP addresses and aliases.
        /// </summary>
        /// <param name="domainName">The domain name to perform NSLookup on.</param>
        /// <returns>A string containing information about the NSLookup results or an error message.</returns>
        public static string NSLookup(string domainName)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(domainName);
                string result = $"NSLookup for {domainName}:\n";
                result += "IP Addresses:\n";

                foreach (IPAddress ipAddress in hostEntry.AddressList)
                {
                    result += $"    {ipAddress}\n";
                }

                result += "Aliases:\n";
                foreach (string alias in hostEntry.Aliases)
                {
                    result += $"    {alias}\n";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"An error occurred while performing the NSLookup: {ex.Message}";
            }
        }
    }
}
