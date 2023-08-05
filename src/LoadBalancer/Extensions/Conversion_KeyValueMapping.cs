using LoadBalancer.Common;
using System;
using System.Linq;

namespace LoadBalancer.Extensions
{
    public interface IKeyValueMap
    {
        void Map(KeyValueCollection snapshot, MapDirection direction);
    }

    public enum MapDirection
    {
        Serialize = 1,
        Materialize = 2,
    }

    public static partial class Conversion
    {
        private static KeyValueCollection SerializeKeyValueMap(IKeyValueMap obj)
        {
            var snapshot = new KeyValueCollection();
            obj.Map(snapshot, direction: MapDirection.Serialize);
            return snapshot;
        }

        private static IKeyValueMap MaterializeKeyValueMap(KeyValueCollection snapshot, IKeyValueMap result)
        {
            result.Map(snapshot, direction: MapDirection.Materialize);
            return result;
        }

        public static void MapProperty<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, ref T value)
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    snapshot.SetValue(key, value);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                        value = snapshot.GetValue<T>(key);
                    break;
            }
        }

        public static void MapProperty<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, Func<T> getValue, Action<T> setValue)
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    var originalValue = getValue();
                    snapshot.SetValue(key, originalValue);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                    {
                        T value = snapshot.GetValue<T>(key);
                        setValue(value);
                    }
                    break;
            }
        }

        public static void MapObject<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, ref T value) where T : IKeyValueMap, new()
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    var backup = SerializeKeyValueMap(value);
                    snapshot.SetValue(key, backup);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                    {
                        var restore = snapshot.GetValue<KeyValueCollection>(key);
                        value = (T)MaterializeKeyValueMap(restore, new T());
                    }
                    break;
            }
        }

        public static void MapObject<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, Func<T> getValue, Action<T> setValue) where T : IKeyValueMap, new()
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    var originalValue = getValue();
                    var backup = SerializeKeyValueMap(originalValue);
                    snapshot.SetValue(key, backup);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                    {
                        var restore = snapshot.GetValue<KeyValueCollection>(key);
                        var value = (T)MaterializeKeyValueMap(restore, new T());
                        setValue(value);
                    }
                    break;
            }
        }

        public static void MapArray<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, ref T[] value) where T : IKeyValueMap, new()
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    var backup = value.Select(m => SerializeKeyValueMap(m)).ToArray();
                    snapshot.SetValue(key, backup);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                    {
                        var restore = snapshot.GetValue<KeyValueCollection[]>(key);
                        value = restore.Select(s => (T)MaterializeKeyValueMap(s, new T())).ToArray();
                    }
                    break;
            }
        }

        public static void MapArray<T>(KeyValueCollection snapshot, MapDirection direction,
            KeyType key, Func<T[]> getValue, Action<T[]> setValue) where T : IKeyValueMap, new()
        {
            switch (direction)
            {
                case MapDirection.Serialize:
                    var originalValue = getValue();
                    var backup = originalValue.Select(m => SerializeKeyValueMap(m)).ToArray();
                    snapshot.SetValue(key, backup);
                    break;
                case MapDirection.Materialize:
                    if (snapshot.ContainsKey(key))
                    {
                        var restore = snapshot.GetValue<KeyValueCollection[]>(key);
                        var value = restore.Select(s => (T)MaterializeKeyValueMap(s, new T())).ToArray();
                        setValue(value);
                    }
                    break;
            }
        }
    }
}
