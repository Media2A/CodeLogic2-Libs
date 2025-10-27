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
        public static void ViewObject(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("Object is null.");
                return;
            }

            Type type = obj.GetType();
            Console.WriteLine($"Object Type: {type.FullName}");

            // Get the properties of the object
            PropertyInfo[] properties = type.GetProperties();
            if (properties.Length > 0)
            {
                Console.WriteLine("Properties:");
                foreach (PropertyInfo property in properties)
                {
                    Console.WriteLine($"{property.Name}: {property.GetValue(obj)}");
                }
            }

            // Get the fields of the object
            FieldInfo[] fields = type.GetFields();
            if (fields.Length > 0)
            {
                Console.WriteLine("Fields:");
                foreach (FieldInfo field in fields)
                {
                    Console.WriteLine($"{field.Name}: {field.GetValue(obj)}");
                }
            }
        }
    }
}
