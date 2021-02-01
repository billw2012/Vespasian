// unset

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

// These types are workarounds to get IL2cpp to work with the save system
// [RegisterSavableType]
public class KeyValuePairRef<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public KeyValuePairRef() {}
    public KeyValuePairRef(TKey key, TValue value)
    {
        this.Key = key;
        this.Value = value;
    }
}

// [DataContract]
// [RegisterSavableType]
public class DictX<TKey, TValue> : IEnumerable<KeyValuePairRef<TKey, TValue>>
{
    private readonly IDictionary<TKey, TValue> impl = new Dictionary<TKey, TValue>();
    
    public DictX() { }

    public DictX(IDictionary<TKey, TValue> dictionary)
    {
        this.impl = new Dictionary<TKey, TValue>(dictionary);
    }

    // public DictX(SerializationInfo info, StreamingContext context)
    // {
    //     Debug.Log($"Deserializing DictX {this.GetType().FullName}");
    //     var array = (KeyValuePairWrapper[])info.GetValue("KeyValuePairs", typeof(KeyValuePairWrapper[]));
    //     foreach (var kv in array)
    //     {
    //         this.Add((TKey)kv.Key, (TValue)kv.Value);
    //     }
    // }
    //
    // public void GetObjectData(SerializationInfo info, StreamingContext context)
    // {
    //     Debug.Log($"Serializing DictX {this.GetType().FullName}");
    //     var array = this.impl.Select(kv => new KeyValuePairWrapper(kv.Key, kv.Value)).ToArray(); 
    //     info.AddValue("KeyValuePairs", (object) array, typeof (KeyValuePairWrapper[]));
    // }

    public void Add(KeyValuePair<TKey, TValue> item) => this.impl.Add(item);

    public void Clear() => this.impl.Clear();

    public bool Contains(KeyValuePair<TKey, TValue> item) => this.impl.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => this.impl.CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) => this.impl.Remove(item);

    public int Count => this.impl.Count;

    public bool IsReadOnly => this.impl.IsReadOnly;

    public void Add(TKey key, TValue value) => this.impl.Add(key, value);
    public void Add(KeyValuePairRef<TKey, TValue> kv) => this.Add(kv.Key, kv.Value);

    public bool ContainsKey(TKey key) => this.impl.ContainsKey(key);

    public bool Remove(TKey key) => this.impl.Remove(key);

    public bool TryGetValue(TKey key, out TValue value) => this.impl.TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get => this.impl[key];
        set => this.impl[key] = value;
    }

    public ICollection<TKey> Keys => this.impl.Keys;

    public ICollection<TValue> Values => this.impl.Values;

    public IEnumerator<KeyValuePairRef<TKey, TValue>> GetEnumerator() => this.impl.Select(kv => new KeyValuePairRef<TKey, TValue>(kv.Key, kv.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}