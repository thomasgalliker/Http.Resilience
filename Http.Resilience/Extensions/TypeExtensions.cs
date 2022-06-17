using System;
using System.Linq;
using System.Reflection;

namespace Http.Resilience.Extensions
{
    /// <summary>
    /// Source:
    /// https://github.com/thomasgalliker/CrossPlatformLibrary/blob/0ea2e849dfccee3f68e719c19daef2eaabec190e/CrossPlatformLibrary/Extensions/TypeExtensions.cs
    /// </summary>
    internal static class TypeExtensions
    {
        internal static string GetFormattedClassName(this Type type, bool useFullName = false)
        {
            var typeName = useFullName ? type.FullName : type.Name;

            var typeInfo = type.GetTypeInfo();

            TryGetInnerElementType(ref typeInfo, out var arrayBrackets);

            if (!typeInfo.IsGenericType)
            {
                return typeName;
            }

            var genericTypeParametersString = typeInfo.IsGenericTypeDefinition ? $"{string.Join(",", typeInfo.GenericTypeParameters.Select(t => string.Empty))}" : $"{string.Join(", ", typeInfo.GenericTypeArguments.Select(t => t.GetFormattedClassName(useFullName)))}";

            if (typeName == null)
            {
                return $"{(string) null}<{genericTypeParametersString}>{arrayBrackets}";
            }

            var iBacktick = typeName.IndexOf('`');
            if (iBacktick > 0)
            {
                typeName = typeName.Remove(iBacktick);
            }

            return $"{typeName}<{genericTypeParametersString}>{arrayBrackets}";
        }

        private static void TryGetInnerElementType(ref TypeInfo type, out string arrayBrackets)
        {
            arrayBrackets = null;
            if (!type.IsArray)
            {
                return;
            }

            do
            {
                arrayBrackets += "[" + new string(',', type.GetArrayRank() - 1) + "]";
                type = type.GetElementType().GetTypeInfo();
            } while (type.IsArray);
        }
    }
}