using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace CL.Core.Utilities
{
    public partial class CLU_Networking
    {
        /// <summary>
        /// Traces the route to a specified IP address with a maximum number of hops.
        /// </summary>
        /// <param name="ipAddress">The IP address to trace the route to.</param>
        /// <param name="maxHops">The maximum number of hops to trace.</param>
        /// <returns>A string containing the trace route results or an error message.</returns>
        public static string TraceRoute(string ipAddress, int maxHops)
        {
            try
            {
                StringBuilder traceResult = new StringBuilder();
                IPAddress destinationAddress = IPAddress.Parse(ipAddress);

                using (Ping ping = new Ping())
                {
                    for (int ttl = 1; ttl <= maxHops; ttl++)
                    {
                        PingOptions options = new PingOptions(ttl, true);
                        StringBuilder hopResult = new StringBuilder();

                        for (int i = 0; i < 3; i++)
                        {
                            PingReply reply = ping.Send(destinationAddress, 5000, new byte[32], options);
                            if (reply.Status == IPStatus.Success)
                            {
                                hopResult.AppendLine($"{ttl}. {reply.Address} : {reply.RoundtripTime}ms");
                                break;
                            }
                            else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                            {
                                hopResult.AppendLine($"{ttl}. *");
                            }
                            else
                            {
                                hopResult.AppendLine($"{ttl}. Error: {reply.Status}");
                            }
                        }

                        traceResult.Append(hopResult);

                        if (hopResult.ToString().Contains(destinationAddress.ToString()))
                        {
                            break;
                        }
                    }
                }

                return traceResult.ToString();
            }
            catch (Exception ex)
            {
                return $"An error occurred while performing the trace route: {ex.Message}";
            }
        }
    }
}
