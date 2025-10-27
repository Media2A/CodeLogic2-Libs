using System;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Reflection;

namespace CL.Core.Utilities
{
    /// <summary>
    /// A utility class for working with assembly information and dynamic object operations.
    /// </summary>
    public partial class CLU_Assemblies
    {
        // Cache to store compiled constructor delegates for types
        private static readonly ConcurrentDictionary<Type, Lazy<Func<object>>> ConstructorCache = new ConcurrentDictionary<Type, Lazy<Func<object>>>();

        // Caches to store compiled delegates for property getters, setters, and method invokers
        private static readonly ConcurrentDictionary<(Type, string), Func<object, object>> GetterCache = new ConcurrentDictionary<(Type, string), Func<object, object>>();

        private static readonly ConcurrentDictionary<(Type, string), Action<object, object>> SetterCache = new ConcurrentDictionary<(Type, string), Action<object, object>>();
        private static readonly ConcurrentDictionary<MethodInfo, Action<object, object[]>> MethodInvokerCache = new ConcurrentDictionary<MethodInfo, Action<object, object[]>>();
        private static readonly ConcurrentDictionary<string, object> AttributeCache = new ConcurrentDictionary<string, object>();

        private static readonly ConcurrentDictionary<Type, Dictionary<string, Action<object, object>>> SetterMapCache = new();

        /// <summary>
        /// Retrieves a dictionary of compiled setters for the properties of the specified type.
        /// </summary>
        /// <param name="type">The type for which to retrieve the compiled setters.</param>
        /// <returns>
        /// A dictionary where the keys are property names and the values are delegates
        /// that set the value of the corresponding property on an object of the specified type.
        /// </returns>
        public static Dictionary<string, Action<object, object>> GetCompiledSetters(Type type)
        {
            return SetterMapCache.GetOrAdd(type, t =>
            {
                var dict = new Dictionary<string, Action<object, object>>();
                foreach (var prop in CLU_Assemblies.GetCachedProperties(t))
                {
                    dict[prop.Name] = CLU_Assemblies.CreateSetter(t, prop.Name);
                }
                return dict;
            });
        }

        // Fast clone cache
        private static readonly ConcurrentDictionary<Type, Delegate> CloneCache = new();

        /// <summary>
        /// Creates a shallow clone of the specified object using compiled expression trees.
        /// </summary>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="source">The object to clone.</param>
        /// <returns>A shallow clone of the object.</returns>
        public static T Clone<T>(T source)
        {
            if (source == null) return default;

            var type = typeof(T);
            var cloner = (Func<T, T>)CloneCache.GetOrAdd(type, t =>
            {
                var parameter = Expression.Parameter(t, "src");

                var bindings = GetCachedProperties(t)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Select(p => Expression.Bind(p, Expression.Property(parameter, p)));

                var body = Expression.MemberInit(Expression.New(t), bindings);
                var lambda = Expression.Lambda<Func<T, T>>(body, parameter);

                return lambda.Compile();
            });

            return cloner(source);
        }

        /// <summary>
        /// Helper method for getting or adding a cache value with a lazy initializer.
        /// </summary>
        private static TValue GetOrAddCache<TKey, TValue>(ConcurrentDictionary<TKey, TValue> cache, TKey key, Func<TKey, TValue> valueFactory)
        {
            return cache.GetOrAdd(key, valueFactory);
        }

        /// <summary>
        /// Creates an instance of the specified type using a compiled expression tree.
        /// </summary>
        /// <param name="type">The type to create an instance of.</param>
        /// <returns>A new instance of the specified type.</returns>
        public static object CreateInstance(Type type)
        {
            var lazyConstructor = ConstructorCache.GetOrAdd(type, t => new Lazy<Func<object>>(() =>
            {
                var newExpression = Expression.New(type);
                var lambda = Expression.Lambda<Func<object>>(Expression.Convert(newExpression, typeof(object)));
                return lambda.Compile();
            }));

            return lazyConstructor.Value();
        }

        /// <summary>
        /// Creates an instance of the specified generic type using a compiled expression tree.
        /// </summary>
        /// <typeparam name="T">The type to create an instance of.</typeparam>
        /// <returns>A new instance of the specified type.</returns>
        public static T CreateInstance<T>() where T : new()
        {
            return (T)CreateInstance(typeof(T));
        }

        /// <summary>
        /// Creates a getter for the specified property of a type using a compiled expression tree.
        /// </summary>
        /// <param name="type">        The type that contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>A delegate that gets the value of the specified property.</returns>
        public static Func<object, object> CreateGetter(Type type, string propertyName)
        {
            var key = (type, propertyName);

            return GetOrAddCache(GetterCache, key, k =>
            {
                var parameter = Expression.Parameter(typeof(object), "instance");
                var convertedParameter = Expression.Convert(parameter, type);
                var property = Expression.PropertyOrField(convertedParameter, propertyName);
                var convertedProperty = Expression.Convert(property, typeof(object));
                var lambda = Expression.Lambda<Func<object, object>>(convertedProperty, parameter);
                return lambda.Compile();
            });
        }

        /// <summary>
        /// Creates a setter for the specified property of a type using a compiled expression tree.
        /// </summary>
        /// <param name="type">        The type that contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>A delegate that sets the value of the specified property.</returns>
        public static Action<object, object> CreateSetter(Type type, string propertyName)
        {
            var key = (type, propertyName);

            return GetOrAddCache(SetterCache, key, k =>
            {
                var instanceParameter = Expression.Parameter(typeof(object), "instance");
                var valueParameter = Expression.Parameter(typeof(object), "value");
                var convertedInstance = Expression.Convert(instanceParameter, type);
                var property = Expression.PropertyOrField(convertedInstance, propertyName);
                var convertedValue = Expression.Convert(valueParameter, property.Type);
                var assign = Expression.Assign(property, convertedValue);
                var lambda = Expression.Lambda<Action<object, object>>(assign, instanceParameter, valueParameter);
                return lambda.Compile();
            });
        }

        /// <summary>
        /// Creates a delegate for invoking a method on an object using a compiled expression tree.
        /// </summary>
        /// <param name="methodInfo">The MethodInfo of the method to invoke.</param>
        /// <returns>A cached delegate that can invoke the method.</returns>
        public static Action<object, object[]> CreateMethodInvoker(MethodInfo methodInfo)
        {
            return GetOrAddCache(MethodInvokerCache, methodInfo, method =>
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                var parameters = methodInfo.GetParameters();
                var argumentExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var index = Expression.Constant(i);
                    var parameterType = parameters[i].ParameterType;
                    var argument = Expression.ArrayIndex(arguments, index);
                    var convert = Expression.Convert(argument, parameterType);
                    argumentExpressions[i] = convert;
                }

                var call = Expression.Call(
                    Expression.Convert(instance, methodInfo.DeclaringType),
                    methodInfo,
                    argumentExpressions);

                var lambda = Expression.Lambda<Action<object, object[]>>(call, instance, arguments);
                return lambda.Compile();
            });
        }

        /// <summary>
        /// Retrieves a custom attribute of the specified type from a member, with caching for performance.
        /// </summary>
        /// <typeparam name="T">The type of the custom attribute.</typeparam>
        /// <param name="memberInfo">The member from which to retrieve the attribute.</param>
        /// <returns>The custom attribute of the specified type, or null if not found.</returns>
        public static T GetCustomAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            string key = $"{memberInfo.DeclaringType.FullName}.{memberInfo.Name}.{typeof(T).FullName}";

            return (T)GetOrAddCache(AttributeCache, key, k => memberInfo.GetCustomAttribute<T>());
        }

        /// <summary>
        /// Retrieves all custom attributes from a member, with caching for performance.
        /// </summary>
        /// <param name="memberInfo">The member from which to retrieve the attributes.</param>
        /// <returns>An array of custom attributes.</returns>
        public static object[] GetCustomAttributes(MemberInfo memberInfo)
        {
            string key = $"{memberInfo.DeclaringType.FullName}.{memberInfo.Name}.AllAttributes";

            return (object[])GetOrAddCache(AttributeCache, key, k => memberInfo.GetCustomAttributes(false));
        }

        // Cache for type properties.
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

        /// <summary>
        /// Retrieves cached public instance properties for the given type.
        /// </summary>
        public static PropertyInfo[] GetCachedProperties(Type type) =>
            PropertiesCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }
}