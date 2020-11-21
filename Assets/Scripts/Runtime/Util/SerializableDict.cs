// using System;
// using System.Collections.Generic;
// using System.Runtime.Serialization;
//
// public class SerializableDict<TKey, TValue>
// {
//     public class KeyValuePair
//     {
//         public TKey key;
//         public TValue value;
//
//         public KeyValuePair(TKey key = default, TValue value = default)
//         {
//             this.key = key;
//             this.value = value;
//         }
//     }
//
//     private List<KeyValuePair> dict;
//
//     public SerializableDict()
//     {
//         this.dict = new List<KeyValuePair>();
//     }
//
//     public SerializableDict(int capacity)
//     {
//         this.dict = new List<KeyValuePair>(capacity);
//     }
//
//     public SerializableDict(IDictionary<TKey, TValue> dictionary)
//     {
//         this.dict = new List<KeyValuePair>(dictionary.Count);
//         foreach (var kv in dictionary)
//         {
//             this.Add(kv.Key, kv.Value);
//         }
//     }
//
//     public SerializableDict(IEnumerable<KeyValuePair<TKey, TValue>> collection)
//     {
//         this.dict = new List<KeyValuePair>(dictionary.Count);
//         foreach (var kv in dictionary)
//         {
//             this.Add(kv.Key, kv.Value);
//         }
//     }
//
//     public void Add(TKey key, TValue value)
//     {
//         this.dict.Add(new KeyValuePair(key, value));
//     }
// }