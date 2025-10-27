using System;
using System.Net;

namespace CL.Core.Utilities
{
    public partial class CLU_Networking
    {
        /// <summary>
        /// Calculates subnet details based on a given subnet mask or prefix length.
        /// </summary>
        /// <param name="subnetInput">The subnet mask (e.g., "255.255.255.0") or prefix length (e.g., 24).</param>
        /// <returns>A string containing subnet details or an error message.</returns>
        public static string SubnetCalculator(string subnetInput)
        {
            try
            {
                IPAddress subnet;
                if (IPAddress.TryParse(subnetInput, out subnet))
                {
                    int prefixLength = SubnetToPrefixLength(subnet);
                    return CalculateSubnetDetails(subnet, prefixLength);
                }
                else if (int.TryParse(subnetInput, out int prefix))
                {
                    IPAddress subnetMask = PrefixLengthToSubnet(prefix);
                    return CalculateSubnetDetails(subnetMask, prefix);
                }
                else
                {
                    return "Invalid subnet input.";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred while performing the subnet calculation: {ex.Message}";
            }
        }

        private static int SubnetToPrefixLength(IPAddress subnet)
        {
            byte[] bytes = subnet.GetAddressBytes();
            return bytes.Length * 8 - Array.LastIndexOf(bytes, (byte)0);
        }

        private static IPAddress PrefixLengthToSubnet(int prefix)
        {
            int subnetLength = 32 - prefix;
            int subnetMask = (int)(0xFFFFFFFF << subnetLength);

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)(subnetMask >> (i * 8));
            }

            return new IPAddress(bytes);
        }

        private static string CalculateSubnetDetails(IPAddress subnet, int prefix)
        {
            uint subnetMask = BitConverter.ToUInt32(subnet.GetAddressBytes(), 0);
            uint hostMask = ~subnetMask;

            int totalAddresses = (int)Math.Pow(2, 32 - prefix);
            int usableAddresses = totalAddresses > 2 ? totalAddresses - 2 : totalAddresses;

            string result = $"Subnet: {subnet}\n";
            result += $"Prefix Length: {prefix}\n";
            result += $"Total Addresses: {totalAddresses}\n";
            result += $"Usable Addresses: {usableAddresses}\n";
            result += $"Network Address: {new IPAddress(subnetMask)}\n";
            result += $"Broadcast Address: {new IPAddress(subnetMask | hostMask)}\n";

            return result;
        }
    }
}
