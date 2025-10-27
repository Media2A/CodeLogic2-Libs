using System.Reflection;

namespace CL.Core.Utilities.Assemblies;

/// <summary>
/// Provides assembly information utilities
/// </summary>
public static class AssemblyHelper
{
    /// <summary>
    /// Returns a dictionary with the Assembly info for a given file.
    /// </summary>
    /// <param name="filePath">The path to the assembly file.</param>
    /// <returns>A dictionary containing assembly information.</returns>
    public static Dictionary<string, string> GetAssemblyInformation(string filePath)
    {
        Assembly assembly = AssemblyLoader.LoadAssembly(filePath);
        var assemblyInfo = new Dictionary<string, string>();

        var name = assembly.GetName();
        assemblyInfo.Add("Name", name.Name ?? string.Empty);
        assemblyInfo.Add("FullName", name.FullName ?? string.Empty);
        assemblyInfo.Add("Version", name.Version?.ToString() ?? string.Empty);
        assemblyInfo.Add("Version.Major", name.Version?.Major.ToString() ?? string.Empty);
        assemblyInfo.Add("Version.Revision", name.Version?.Revision.ToString() ?? string.Empty);
        assemblyInfo.Add("Version.Minor", name.Version?.Minor.ToString() ?? string.Empty);

        var description = assembly
            .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
            .OfType<AssemblyDescriptionAttribute>()
            .FirstOrDefault()?
            .Description ?? string.Empty;
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
        return assembly.GetName().Name ?? string.Empty;
    }

    /// <summary>
    /// Gets the version of the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly version is to be retrieved.</param>
    /// <returns>The version of the assembly as a string.</returns>
    public static string GetAssemblyVersion(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Gets the description of the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly description is to be retrieved.</param>
    /// <returns>The description of the assembly.</returns>
    public static string GetAssemblyDescription(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var descriptionAttribute = (AssemblyDescriptionAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyDescriptionAttribute));
        return descriptionAttribute?.Description ?? "No Description";
    }

    /// <summary>
    /// Gets the title of the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly title is to be retrieved.</param>
    /// <returns>The title of the assembly.</returns>
    public static string GetAssemblyTitle(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var titleAttribute = (AssemblyTitleAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyTitleAttribute));
        return titleAttribute?.Title ?? "No Title";
    }

    /// <summary>
    /// Gets the company name associated with the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly company name is to be retrieved.</param>
    /// <returns>The company name associated with the assembly.</returns>
    public static string GetAssemblyCompany(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var companyAttribute = (AssemblyCompanyAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyCompanyAttribute));
        return companyAttribute?.Company ?? "No Company";
    }

    /// <summary>
    /// Gets the product name associated with the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly product name is to be retrieved.</param>
    /// <returns>The product name associated with the assembly.</returns>
    public static string GetAssemblyProduct(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var productAttribute = (AssemblyProductAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyProductAttribute));
        return productAttribute?.Product ?? "No Product";
    }

    /// <summary>
    /// Gets the configuration information associated with the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly configuration information is to be retrieved.</param>
    /// <returns>The configuration information associated with the assembly.</returns>
    public static string GetAssemblyConfiguration(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var configAttribute = (AssemblyConfigurationAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyConfigurationAttribute));
        return configAttribute?.Configuration ?? "No Configuration";
    }

    /// <summary>
    /// Gets the trademark information associated with the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly trademark information is to be retrieved.</param>
    /// <returns>The trademark information associated with the assembly.</returns>
    public static string GetAssemblyTrademark(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        var trademarkAttribute = (AssemblyTrademarkAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyTrademarkAttribute));
        return trademarkAttribute?.Trademark ?? "No Trademark";
    }

    /// <summary>
    /// Gets the culture information associated with the assembly for the provided object.
    /// </summary>
    /// <param name="obj">The object whose assembly culture information is to be retrieved.</param>
    /// <returns>The culture information associated with the assembly.</returns>
    public static string GetAssemblyCulture(object obj)
    {
        Assembly assembly = obj.GetType().Assembly;
        return assembly.GetName().CultureInfo?.DisplayName ?? string.Empty;
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

        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }
}
