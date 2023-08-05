using LoadBalancer.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoadBalancer.Common
{
    public delegate void KeyValueChangedHandler(KeyType key, object oldValue, object newValue);
    public class KeyValueCollection : IEnumerable<KeyValuePair<KeyType, object>>
    {
        private readonly object lockObject = new object();
        private readonly Dictionary<KeyType, object> collection = new Dictionary<KeyType, object>();

        public event KeyValueChangedHandler OnValueChanged;

        public KeyValueCollection() { }
        public KeyValueCollection(KeyValueCollection source) => Merge(source);

        public int Count { get { lock (lockObject) return collection.Count; } }

        public object this[KeyType key]
        {
            get { lock (lockObject) return collection[key]; }
            set { lock (lockObject) collection[key] = value; }
        }

        public void Clear()
        {
            lock (lockObject)
                collection.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            lock (lockObject)
                return collection.ContainsKey(key);
        }

        public bool TryGetValue(KeyType key, out object value)
        {
            lock (lockObject)
                return collection.TryGetValue(key, out value);
        }

        public List<KeyType> Keys { get { lock (lockObject) return collection.Keys.ToList(); } }

        private List<KeyValuePair<KeyType, object>> All()
        {
            lock (lockObject)
                return collection.ToList();
        }

        public T GetValue<T>(KeyType key, T defaultValue = default)
        {
            if (!TryGetValue(key, out var baseValue))
                return defaultValue;

            var value = (T)Conversion.ConvertSmart(baseValue, typeof(T));
            return value;
        }

        public void Add(KeyType key, object value)
        {
            lock (lockObject)
            {
                collection.Add(key, value);
                if (OnValueChanged != null)
                    OnValueChanged(key, null, value);
            }
        }

        public void SetValue<T>(KeyType key, T value)
        {
            lock (lockObject)
            {
                if (OnValueChanged == null)
                    collection[key] = value;
                else
                {
                    collection.TryGetValue(key, out var oldValue);
                    collection[key] = value;
                    OnValueChanged(key, oldValue, value);
                }
            }
        }

        public void Remove(KeyType key)
        {
            lock (lockObject)
                collection.Remove(key);
        }

        public bool Match(KeyValueCollection properties)
        {
            lock (lockObject)
            {
                foreach (var pair in properties.All())
                {
                    var required = pair.Value;

                    if (!TryGetValue(pair.Key, out var value))
                        return false;

                    if (!Comparison.EqualsSmart(required, value))
                        return false;
                }
            }

            return true;
        }

        public void Merge(KeyValueCollection properties)
        {
            if (properties == null) return;

            lock (lockObject)
            {
                foreach (var pair in properties.All())
                {
                    SetValue(pair.Key, pair.Value);
                }
            }
        }

        public T Extract<T>() where T : KeyValueCollection, new()
        {
            lock (lockObject)
            {
                var result = new T();
                foreach (var key in result.SupportedKeys)
                {
                    if (TryGetValue(key, out var value))
                        result.Add(key, value);
                }

                return result;
            }
        }

        public KeyValueCollection Extract(IEnumerable<KeyType> keys)
        {
            lock (lockObject)
            {
                var result = new KeyValueCollection();
                foreach (var key in keys)
                {
                    if (TryGetValue(key, out var value))
                        result.Add(key, value);
                }

                return result;
            }
        }

        public virtual IEnumerable<KeyType> SupportedKeys => new KeyType[] { };

        public IEnumerator<KeyValuePair<KeyType, object>> GetEnumerator() => All().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => All().GetEnumerator();

        public override string ToString() => $"{Count} items";

        #region Change tracking

        private KeyValueCollection commited;

        private KeyValueCollection changes;
        public bool IsChangeTrackingOn { get { lock (lockObject) return changes != null; } }
        private bool disableChangeTracking;

        public bool HasChanges { get { lock (lockObject) return changes != null && changes.Any(); } }

        public bool HasChangeAny(params KeyType[] keys)
        {
            lock (lockObject)
                return changes != null && changes.Keys.Intersect(keys).Any();
        }

        public bool HasChangeExcept(params KeyType[] keys)
        {
            lock (lockObject)
                return changes != null && changes.Keys.Except(keys).Any();
        }

        public void ExecuteWithoutTracking(Action action)
        {
            lock (lockObject)
            {
                try
                {
                    disableChangeTracking = true;
                    action();
                }
                finally
                {
                    disableChangeTracking = false;
                }
            }
        }

        public void EnableChangeTracking()
        {
            lock (lockObject)
            {
                if (IsChangeTrackingOn) return;
                changes = new KeyValueCollection();
                commited = new KeyValueCollection(this);
                OnValueChanged += KeyValueCollection_OnValueChanged;
            }
        }

        public void DisableChangeTracking()
        {
            lock (lockObject)
            {
                if (!IsChangeTrackingOn) return;
                changes = null;
                OnValueChanged -= KeyValueCollection_OnValueChanged;
            }
        }

        public void RemoveTracked(KeyType key)
        {
            lock (lockObject)
                if (commited != null)
                    commited.Remove(key);
        }

        private void KeyValueCollection_OnValueChanged(KeyType key, object oldValue, object value)
        {
            if (disableChangeTracking) return;

            if (!commited.TryGetValue(key, out var original) || !Equals(original, value))
                changes[key] = value;
        }

        public bool CommitChanges(Action<KeyValueCollection> onCommit)
        {
            lock (lockObject)
            {
                if (HasChanges)
                {
                    onCommit(changes);
                    commited.Merge(changes);
                    changes = new KeyValueCollection();
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
