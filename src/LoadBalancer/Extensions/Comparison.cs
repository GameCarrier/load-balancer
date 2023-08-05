using LoadBalancer.Common;
using System;
using System.Reflection;

namespace LoadBalancer.Extensions
{
    public static partial class Comparison
    {
        public static bool EqualsSmart(object a, object b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a is KeyValueCollection && b is KeyValueCollection)
                return EqualsSmart((KeyValueCollection)a, (KeyValueCollection)b);

            if (a.GetType() != b.GetType())
                return false;

            if (a is ICompressed)
                return EqualsSmart(((ICompressed)a).Value, ((ICompressed)b).Value);

            if (a is IKeyValueMap)
                return EqualsSmart((IKeyValueMap)a, (IKeyValueMap)b);

            if (a is string)
                return (string)a == (string)b;

            if (a is byte[])
                return EqualsSmart((byte[])a, (byte[])b);

            if (a is Array)
                return EqualsSmart((Array)a, (Array)b);

            if (!Serialization.HasSerializerForType(a.GetType()))
            {
                foreach (var property in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var valuea = property.GetValue(a);
                    var valueb = property.GetValue(b);
                    if (!EqualsSmart(valuea, valueb))
                        return false;
                }

                return true;
            }

            return Equals(a, b);
        }

        public static bool EqualsSmart(IKeyValueMap a, IKeyValueMap b) => EqualsSmart(a.Serialize(), b.Serialize());

        public static bool EqualsSmart(KeyValueCollection a, KeyValueCollection b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Count != b.Count)
                return false;

            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out var value))
                    return false;

                if (!EqualsSmart(pair.Value, value))
                    return false;
            }

            return true;
        }

        public static bool EqualsSmart(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        public static bool EqualsSmart(Array a, Array b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!EqualsSmart(a.GetValue(i), b.GetValue(i)))
                    return false;
            }

            return true;
        }
    }
}
