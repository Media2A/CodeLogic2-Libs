using System;
using System.Net;
using System.Net.NetworkInformation;

namespace CL.Core.Utilities
{
    public partial class CLU_Networking
    {
        /// <summary>
        /// Pings the specified IP address multiple times and returns ping statistics.
        /// </summary>
        /// <param name="ipAddress">The IP address to ping.</param>
        /// <param name="repeatCount">The number of times to send a ping request.</param>
        /// <returns>A string containing ping statistics or an error message.</returns>
        public static string PingIPAddress(string ipAddress, int repeatCount)
        {
            try
            {
                long totalRoundtripTime = 0;
                int successfulCount = 0;

                using (Ping ping = new Ping())
                {
                    for (int i = 0; i < repeatCount; i++)
                    {
                        PingReply reply = ping.Send(ipAddress);
                        if (reply.Status == IPStatus.Success)
                        {
                            totalRoundtripTime += reply.RoundtripTime;
                            successfulCount++;
                        }
                    }
                }

                if (successfulCount > 0)
                {
                    double averageRoundtripTime = (double)totalRoundtripTime / successfulCount;
                    string result = $"Ping statistics for {ipAddress}:\n";
                    result += $"    Packets sent: {repeatCount}\n";
                    result += $"    Packets received: {successfulCount}\n";
                    result += $"    Packet loss: {100 - (successfulCount * 100 / repeatCount)}%\n";
                    result += $"    Average roundtrip time: {averageRoundtripTime}ms\n";
                    return result;
                }
                else
                {
                    string result = $"Ping statistics for {ipAddress}:\n";
                    result += $"    Packets sent: {repeatCount}\n";
                    result += $"    Packets received: {successfulCount}\n";
                    result += $"    Packet loss: 100%\n";
                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred while pinging the IP address: {ex.Message}";
            }
        }
    }
}
