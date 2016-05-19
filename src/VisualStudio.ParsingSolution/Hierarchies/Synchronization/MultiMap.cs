using System;
using System.Collections.Generic;

namespace VsxFactory.Modeling.VisualStudio
{
    /// <summary>
    /// Dictionary such as the value of a key is a collection (List&lt;V&gt;)
    /// This MultiMap is suitable for implementation of qualified UML association end
    /// of multiple cardinality 
    /// </summary>
    [Serializable]
    public class MultiMap<K, V> : Dictionary<K, List<V>>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public MultiMap()
        {
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        public MultiMap(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Use a comparer in a multimap that is passed to the hashtable
        /// </summary>
        /// <param name="comparer">Comparer</param>
        public MultiMap(IEqualityComparer<K> comparer)
            : base(comparer)
        {
        }


        /// <summary>
        /// Add a value to the MultiMap
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value to be added to values of key</param>
        public void Add(K key, V value)
        {
            // If key already exists, add value to List<V> if not already present
            if (base.ContainsKey(key))
            {
                List<V> values = (List<V>)base[key];
                if (!values.Contains(value))
                    values.Add(value);
            }

            // Key doesn't exist : Create an arrayList, add value to it, and add the
            // key, values pair to the hashtable
            else
            {
                List<V> values = new List<V>();
                values.Add(value);
                base.Add(key, values);
            }
        }

        /// <summary>
        /// Remove a mapping (key, object)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value to remove</param>
        public void Remove(K key, V value)
        {
            // If key already exists, add value to ArrayList if not already present
            if (base.ContainsKey(key))
            {
                List<V> values = (List<V>)base[key];
                if (values.Contains(value))
                    values.Remove(value);

                // If there does not remain any values, remove the key
                if (values.Count == 0)
                    base.Remove(key);
            }
        }

        /// <summary>
        /// Get first key of the collection
        /// </summary>
        /// <returns>First key of the collection if the collection has almost one element, or <c>null</c> otherwise</returns>
        public K GetFirstKey()
        {
            System.Collections.IDictionaryEnumerator it = base.GetEnumerator();
            it.Reset();
            if (it.MoveNext())
                return (K)it.Key;
            else
                return default(K);
        }

        /// <summary><para>
        /// Tells if <see cref="T:DirectSim.Designs.Collections.MultiMap" /> contains a specified value.
        /// </para></summary><param name="value">
        /// Value to be found in <see cref="T:DirectSim.Designs.Collections.MultiMap" />. The value might be <see langword="null" />.
        /// </param><returns><para><see langword="true" /> if <see cref="T:DirectSim.Designs.Collections.MultiMap" /> contains an element that equals <paramref name="value" />  ; otherwise <see langword="false" />.
        /// </para></returns>
        public bool ContainsValue(V value)
        {
            foreach (List<V> l in base.Values)
                if (l.Contains(value))
                    return true;
            return false;
        }

        /// <summary>
        /// Accesses the objects from a key
        /// </summary>
        /// <param name="key">Key we want the objects associated with</param>
        /// <value>Collection of objects associated with the specified key</value>
        public new V[] this[K key]
        {
            get
            {
                List<V> values = (List<V>)base[key];
                if (values != null)
                    return values.ToArray();
                else
                    return null;
            }
            set
            {
                base[key] = new List<V>(value);
            }
        }

        /// <summary><para>
        /// Get a <see cref="T:System.Collections.ICollection" /> containing all the values of the <see cref="T:DirectSim.Designs.Collections.MultiMap" />.
        /// </para></summary>
        /// <value>Returns a collection of all values of the multimap, that is all values of all keys of the multimap</value>
        public new ICollection<V> Values
        {
            get
            {
                List<V> l = new List<V>();
                foreach (List<V> values in base.Values)
                    foreach (V o in values)
                    {
                        if (!l.Contains(o))
                            l.Add(o);
                    }
                return l;
            }
        }
    }
}
