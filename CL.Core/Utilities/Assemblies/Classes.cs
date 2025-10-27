using System.Reflection;

namespace CL.Core.Utilities
{
    /// <summary>
    /// A utility class for working with assembly information.
    /// </summary>
    public partial class CLU_Assemblies
    {
        public static T ConvertToClass<T>(dynamic dynamicObject) where T : new()
        {
            if (dynamicObject == null)
            {
                throw new Exception("Dynamic object is null.");
            }

            T instance = new T();
            Type type = instance.GetType();

            foreach (var property in GetPropertyNames(dynamicObject))
            {
                PropertyInfo propInfo = type.GetProperty(property);
                if (propInfo != null)
                {
                    object value = dynamicObject[property];
                    if (value != null)
                    {
                        propInfo.SetValue(instance, Convert.ChangeType(value, propInfo.PropertyType));
                    }
                    else
                    {
                        throw new Exception("Property has a null value.");
                    }
                }
            }

            return instance;
        }

        public static IEnumerable<string> GetPropertyNames(dynamic obj)
        {
            return obj.GetDynamicMemberNames();
        }
    }
}