using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Hosting;

namespace CL.Core.Utilities
{
    /// <summary>
    /// A utility class for working with assembly information.
    /// </summary>
    public partial class CLU_Assemblies
    {
        /// <summary>
        /// Gets the version of the .NET runtime that is currently executing.
        /// </summary>
        /// <returns>A string representing the .NET runtime version.</returns>
        public static string GetDotNetVersion()
        {
            return RuntimeInformation.FrameworkDescription;
        }

        /// <summary>
        /// Gets the detailed version of ASP.NET Core.
        /// </summary>
        /// <returns>A string representing the ASP.NET Core version, including preview details if available.</returns>
        public static string GetDotNetAspVersion(bool includeBuildMetadata = false)
        {
            try
            {
                Assembly aspNetCoreAssembly = typeof(WebHostBuilder).Assembly;
                string informationalVersion = aspNetCoreAssembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion ?? "N/A";

                if (!includeBuildMetadata)
                {
                    int plusIndex = informationalVersion.IndexOf('+');
                    if (plusIndex > -1)
                    {
                        informationalVersion = informationalVersion.Substring(0, plusIndex);
                    }
                }

                return informationalVersion;
            }
            catch
            {
                return "N/A";
            }
        }

    }
}
