using System;

namespace LoadBalancer.Extensions
{
    public static partial class Conversion
    {
        public static object ConvertSmart(object value, Type type)
        {
            if (value == null)
                return default;

            if (value.GetType() == type)
                return value;

            if (value is Array valueArray && type.IsArray)
            {
                var elementType = type.GetElementType();
                var result = Array.CreateInstance(elementType, valueArray.Length);
                for (int i = 0; i < valueArray.Length; i++)
                {
                    var element = valueArray.GetValue(i);
                    var converted = ConvertSmart(element, elementType);
                    result.SetValue(converted, i);
                }
                return result;
            }

            return Convert.ChangeType(value, type);
        }
    }
}
