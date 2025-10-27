using System;
using System.IO;
using System.Reflection;

namespace CL.Core.Utilities
{
    public partial class CLU_Assemblies
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

            // Get the assembly name for comparison
            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);

            // Check if the assembly is already loaded in the current AppDomain
            var existingAssembly = AppDomain.CurrentDomain
                                            .GetAssemblies()
                                            .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));

            if (existingAssembly != null)
            {
                // Assembly is already loaded, return the existing assembly
                return existingAssembly;
            }

            // If not loaded, load the assembly
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
        private static object InvokeMethod(string fileName, string typeName, string methodName, object[] parameters)
        {
            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (!File.Exists(file))
            {
                throw new FileNotFoundException("File not found: " + file);
            }

            // Load the assembly using the LoadAssembly method
            Assembly assembly = LoadAssembly(file);

            Type type = assembly.GetType(typeName);

            if (type == null)
            {
                throw new TypeLoadException("Type not found: " + typeName);
            }

            var method = type.GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException("Method not found: " + methodName);
            }

            object instance = Activator.CreateInstance(type);

            var result = method.Invoke(instance, parameters);

            return result;
        }

        /// <summary>
        /// Invokes a method from an external assembly and returns the result as an object.
        /// </summary>
        /// <param name="invFileName">The name of the assembly file.</param>
        /// <param name="invType">The fully-qualified type name.</param>
        /// <param name="invMethodName">The method name.</param>
        /// <returns>The result of the method invocation as an object.</returns>
        public static object GetObjectInvokeDll(string invFileName, string invType, string invMethodName)
        {
            return InvokeMethod(invFileName, invType, invMethodName, null);
        }

        /// <summary>
        /// Invokes a method from an external assembly and returns the result as a string.
        /// </summary>
        /// <param name="invFileName">The name of the assembly file.</param>
        /// <param name="invType">The fully-qualified type name.</param>
        /// <param name="invMethodName">The method name.</param>
        /// <returns>The result of the method invocation as a string.</returns>
        public static string GetStringInvokeDll(string invFileName, string invType, string invMethodName)
        {
            var result = InvokeMethod(invFileName, invType, invMethodName, null);
            return result.ToString();
        }

        /// <summary>
        /// Invokes a method from an external assembly with parameters and returns the result as an object.
        /// </summary>
        /// <param name="invFileName">The name of the assembly file.</param>
        /// <param name="invType">The fully-qualified type name.</param>
        /// <param name="invMethodName">The method name.</param>
        /// <param name="invParm">An array of parameters to pass to the method.</param>
        /// <returns>The result of the method invocation as an object.</returns>
        public static object GetObjectInvokeDllWithParm(string invFileName, string invType, string invMethodName, object[] invParm)
        {
            return InvokeMethod(invFileName, invType, invMethodName, invParm);
        }

        /// <summary>
        /// Invokes a method from an external assembly with parameters and returns the result as a string.
        /// </summary>
        /// <param name="invFileName">The name of the assembly file.</param>
        /// <param name="invType">The fully-qualified type name.</param>
        /// <param name="invMethodName">The method name.</param>
        /// <param name="invParm">An array of parameters to pass to the method.</param>
        /// <returns>The result of the method invocation as a string.</returns>
        public static string GetStringInvokeDllWithParm(string invFileName, string invType, string invMethodName, object[] invParm)
        {
            var result = InvokeMethod(invFileName, invType, invMethodName, invParm);
            return result.ToString();
        }
    }
}
