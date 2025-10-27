using System.Reflection;
using System.Resources;

namespace CL.Core.Utilities
{
    public partial class CLU_Assemblies
    {
        /// <summary>
        /// Gets a binary resource from an assembly using its resource name and the path to the assembly file.
        /// </summary>
        /// <param name="resourceName">The name of the resource to retrieve.</param>
        /// <param name="dllPath">The path to the assembly file that contains the resource.</param>
        /// <returns>A byte array representing the binary resource data, or null if the resource is not found.</returns>
        public static byte[] GetBinaryResource(string resourceName, string dllPath)
        {
            // Load the assembly using the new LoadAssembly method.
            Assembly assembly = LoadAssembly(dllPath);

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        resourceStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a string resource from an assembly using its resource name and the path to the assembly file.
        /// </summary>
        /// <param name="resourceName">The name of the resource to retrieve.</param>
        /// <param name="dllPath">The path to the assembly file that contains the resource.</param>
        /// <returns>The string resource data, or null if the resource is not found or is not a string.</returns>
        public static string GetStringResourceFromFile(string resourceName, string dllPath)
        {
            // Load the assembly using the new LoadAssembly method.
            Assembly assembly = LoadAssembly(dllPath);

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream != null)
                {
                    using (var reader = new StreamReader(resourceStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a resource of type <typeparamref name="T"/> from the calling assembly using its namespace and resource name.
        /// </summary>
        /// <typeparam name="T">The type of the resource to retrieve.</typeparam>
        /// <param name="namespaceName">The namespace where the resource is located.</param>
        /// <param name="resourceName">The name of the resource to retrieve.</param>
        /// <returns>The resource as type <typeparamref name="T"/>, or null if the resource is not found.</returns>
        public static T GetResource<T>(string namespaceName, string resourceName)
        {
            // Get the calling assembly.
            Assembly callingAssembly = Assembly.GetCallingAssembly();

            // Construct the full resource name including the namespace.
            string fullResourceName = $"{namespaceName}.{resourceName}";

            // Get the resource manager for the specified resource file.
            ResourceManager resourceManager = new ResourceManager(fullResourceName, callingAssembly);

            // Attempt to retrieve the resource.
            if (typeof(T) == typeof(string))
            {
                // Special case for string resources.
                return (T)(object)resourceManager.GetString(resourceName);
            }
            else
            {
                // For non-string resources, use the GetObject method.
                return (T)resourceManager.GetObject(resourceName);
            }
        }
    }
}
