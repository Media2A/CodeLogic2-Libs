using System.Reflection;

namespace CL.Core.Utilities.Assemblies;

/// <summary>
/// Provides utilities for loading and invoking assemblies
/// </summary>
public static class AssemblyLoader
{
    /// <summary>
    /// Loads an assembly from the specified path, if it has not already been loaded into the current application domain.
    /// </summary>
    /// <param name="assemblyPath">The file path of the assembly to load.</param>
    /// <returns>
    /// The loaded assembly if it was not already loaded; otherwise, the existing assembly from the current application domain.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the assembly path is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in assemblyPath is not found.</exception>
    /// <exception cref="BadImageFormatException">Thrown when the file is not a valid assembly.</exception>
    public static Assembly LoadAssembly(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
        {
            throw new ArgumentNullException(nameof(assemblyPath), "The assembly path cannot be null or empty.");
        }

        var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);

        var existingAssembly = AppDomain.CurrentDomain
                                        .GetAssemblies()
                                        .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));

        if (existingAssembly != null)
        {
            return existingAssembly;
        }

        return Assembly.LoadFrom(assemblyPath);
    }

    /// <summary>
    /// Invokes a method from an external assembly.
    /// </summary>
    /// <param name="fileName">The name of the assembly file, including the extension (e.g., MyLibrary.dll).</param>
    /// <param name="typeName">The fully-qualified type name that contains the method (e.g., MyNamespace.MyClass).</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">An array of method parameters, or null if the method has no parameters.</param>
    /// <returns>The result of the method invocation.</returns>
    private static object InvokeMethod(string fileName, string typeName, string methodName, object[]? parameters)
    {
        var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        if (!File.Exists(file))
        {
            throw new FileNotFoundException("File not found: " + file);
        }

        Assembly assembly = LoadAssembly(file);
        Type? type = assembly.GetType(typeName);

        if (type == null)
        {
            throw new TypeLoadException("Type not found: " + typeName);
        }

        var method = type.GetMethod(methodName);

        if (method == null)
        {
            throw new MissingMethodException("Method not found: " + methodName);
        }

        object? instance = Activator.CreateInstance(type);
        var result = method.Invoke(instance, parameters);

        return result ?? throw new InvalidOperationException("Method returned null");
    }

    /// <summary>
    /// Invokes a method from an external assembly and returns the result as an object.
    /// </summary>
    /// <param name="fileName">The name of the assembly file.</param>
    /// <param name="typeName">The fully-qualified type name.</param>
    /// <param name="methodName">The method name.</param>
    /// <returns>The result of the method invocation as an object.</returns>
    public static object InvokeMethod(string fileName, string typeName, string methodName)
    {
        return InvokeMethod(fileName, typeName, methodName, null);
    }

    /// <summary>
    /// Invokes a method from an external assembly and returns the result as a string.
    /// </summary>
    /// <param name="fileName">The name of the assembly file.</param>
    /// <param name="typeName">The fully-qualified type name.</param>
    /// <param name="methodName">The method name.</param>
    /// <returns>The result of the method invocation as a string.</returns>
    public static string InvokeMethodAsString(string fileName, string typeName, string methodName)
    {
        var result = InvokeMethod(fileName, typeName, methodName, null);
        return result.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Invokes a method from an external assembly with parameters and returns the result as an object.
    /// </summary>
    /// <param name="fileName">The name of the assembly file.</param>
    /// <param name="typeName">The fully-qualified type name.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="parameters">An array of parameters to pass to the method.</param>
    /// <returns>The result of the method invocation as an object.</returns>
    public static object InvokeMethodWithParameters(string fileName, string typeName, string methodName, object[] parameters)
    {
        return InvokeMethod(fileName, typeName, methodName, parameters);
    }

    /// <summary>
    /// Invokes a method from an external assembly with parameters and returns the result as a string.
    /// </summary>
    /// <param name="fileName">The name of the assembly file.</param>
    /// <param name="typeName">The fully-qualified type name.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="parameters">An array of parameters to pass to the method.</param>
    /// <returns>The result of the method invocation as a string.</returns>
    public static string InvokeMethodWithParametersAsString(string fileName, string typeName, string methodName, object[] parameters)
    {
        var result = InvokeMethod(fileName, typeName, methodName, parameters);
        return result.ToString() ?? string.Empty;
    }
}
