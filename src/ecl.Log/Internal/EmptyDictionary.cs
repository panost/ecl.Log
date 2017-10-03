using System;
using System.Collections;
using System.Collections.Generic;

namespace ecl.Log {
    class EmptyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> {
        public static readonly IReadOnlyDictionary<TKey, TValue> Instance = new EmptyDictionary<TKey, TValue>();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
            return ( (IEnumerable<KeyValuePair<TKey, TValue>>)
                Array.Empty<KeyValuePair<TKey, TValue>>() )
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Array.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => 0;

        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey( TKey key ) => false;

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue( TKey key, out TValue value ) {
            value = default;
            return false;
        }

        TValue IReadOnlyDictionary<TKey, TValue>.this[ TKey key ] => throw new KeyNotFoundException();

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Array.Empty<TKey>();

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Array.Empty<TValue>();
    }
}
