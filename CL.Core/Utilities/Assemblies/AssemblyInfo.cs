using System.Reflection;
using System.Resources;
using System.Collections.Generic;
using System.Linq;

namespace CL.Core.Utilities
{
    /// <summary>
    /// A utility class for working with assembly information.
    /// </summary>
    public partial class CLU_Assemblies
    {
        /// <summary>
        /// Returns a dictionary with the Assembly info for a given file.
        /// </summary>
        /// <param name="File">The path to the assembly file.</param>
        /// <returns>A dictionary containing assembly information.</returns>
        public static Dictionary<string, string> GetAssemblyInformation(string File)
        {
            // Load the assembly using the new LoadAssembly method.
            Assembly assembly = LoadAssembly(File);
            Dictionary<string, string> assemblyInfo = new Dictionary<string, string>();

            // Retrieve assembly information and add it to the dictionary.
            assemblyInfo.Add("Name", assembly.GetName().Name);
            assemblyInfo.Add("FullName", assembly.GetName().FullName);
            assemblyInfo.Add("Version", assembly.GetName().Version.ToString());
            assemblyInfo.Add("Version.Major", assembly.GetName().Version.Major.ToString());
            assemblyInfo.Add("Version.Revision", assembly.GetName().Version.Revision.ToString());
            assemblyInfo.Add("Version.Minor", assembly.GetName().Version.Minor.ToString());

            // Get the AssemblyDescriptionAttribute for the assembly.
            var description = assembly
                .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                .OfType<AssemblyDescriptionAttribute>()
                .FirstOrDefault()?
                .Description ?? "";
            assemblyInfo.Add("Description", description);

            return assemblyInfo;
        }

        /// <summary>
        /// Gets the name of the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly name is to be retrieved.</param>
        /// <returns>The name of the assembly.</returns>
        public static string GetAssemblyName(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            return assembly.GetName().Name;
        }

        /// <summary>
        /// Gets the version of the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly version is to be retrieved.</param>
        /// <returns>The version of the assembly as a string.</returns>
        public static string GetAssemblyVersion(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            return assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Gets the description of the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly description is to be retrieved.</param>
        /// <returns>The description of the assembly.</returns>
        public static string GetAssemblyDescription(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyDescriptionAttribute descriptionAttribute = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute));
            return descriptionAttribute != null ? descriptionAttribute.Description : "No Description";
        }

        /// <summary>
        /// Gets the title of the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly title is to be retrieved.</param>
        /// <returns>The title of the assembly.</returns>
        public static string GetAssemblyTitle(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
            return titleAttribute != null ? titleAttribute.Title : "No Title";
        }

        /// <summary>
        /// Gets the company name associated with the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly company name is to be retrieved.</param>
        /// <returns>The company name associated with the assembly.</returns>
        public static string GetAssemblyCompany(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyCompanyAttribute companyAttribute = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute));
            return companyAttribute != null ? companyAttribute.Company : "No Company";
        }

        /// <summary>
        /// Gets the product name associated with the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly product name is to be retrieved.</param>
        /// <returns>The product name associated with the assembly.</returns>
        public static string GetAssemblyProduct(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyProductAttribute productAttribute = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
            return productAttribute != null ? productAttribute.Product : "No Product";
        }

        /// <summary>
        /// Gets the configuration information associated with the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly configuration information is to be retrieved.</param>
        /// <returns>The configuration information associated with the assembly.</returns>
        public static string GetAssemblyConfiguration(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyConfigurationAttribute configAttribute = (AssemblyConfigurationAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyConfigurationAttribute));
            return configAttribute != null ? configAttribute.Configuration : "No Configuration";
        }

        /// <summary>
        /// Gets the trademark information associated with the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly trademark information is to be retrieved.</param>
        /// <returns>The trademark information associated with the assembly.</returns>
        public static string GetAssemblyTrademark(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            AssemblyTrademarkAttribute trademarkAttribute = (AssemblyTrademarkAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTrademarkAttribute));
            return trademarkAttribute != null ? trademarkAttribute.Trademark : "No Trademark";
        }

        /// <summary>
        /// Gets the culture information associated with the assembly for the provided object.
        /// </summary>
        /// <param name="obj">The object whose assembly culture information is to be retrieved.</param>
        /// <returns>The culture information associated with the assembly.</returns>
        public static string GetAssemblyCulture(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;
            return assembly.GetName().CultureInfo.DisplayName;
        }

        /// <summary>
        /// Gets the informational version string of the assembly that contains the specified object.
        /// </summary>
        /// <param name="obj">An object whose assembly's informational version will be retrieved.</param>
        /// <returns>
        /// The <see cref="AssemblyInformationalVersionAttribute.InformationalVersion"/> if present; 
        /// otherwise, falls back to the assembly's version string.
        /// </returns>
        public static string GetAssemblyInformationalVersion(object obj)
        {
            Assembly assembly = obj.GetType().Assembly;

            var infoVersionAttr = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                                         .OfType<AssemblyInformationalVersionAttribute>()
                                         .FirstOrDefault();

            if (infoVersionAttr != null)
            {
                return infoVersionAttr.InformationalVersion;
            }

            // fallback to assembly version if InformationalVersion not found
            return assembly.GetName().Version.ToString();
        }

    }
}
