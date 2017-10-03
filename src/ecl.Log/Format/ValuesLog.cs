using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ecl.Log.Format {
    class ValuesLog : IReadOnlyList<KeyValuePair<string, object>> {
        private readonly ValuesFormatter _formatter;
        private readonly IReadOnlyList<object> _values;
        private string _message;

        private ValuesLog( ValuesFormatter formatter, IReadOnlyList<object> values ) {
            _formatter = formatter;
            _values = values;
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            for ( int i = 0; i < _formatter.Count; i++ ) {
                yield return this[ i ];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ( (IEnumerable<KeyValuePair<string, object>>)this ).GetEnumerator();
        }

        public int Count => _formatter.Count;

        public KeyValuePair<string, object> this[ int index ] {
            get {
                return new KeyValuePair<string, object>( _formatter[ index ], _values[ index ] );
            }
        }

        public static ValuesLog Get( string message, object[] args ) {
            if ( args != null ) {
                int length= args.Length;
                if ( length > 0 ) {
                    ValuesFormatter f = ValuesFormatter.Get( message );
                    if ( f.Count > length ) {
                        throw new ArgumentException( message, nameof(args) );
                    }
                    return new ValuesLog( f, args );
                }
            }
            return null;

        }

        public override string ToString() {
            if ( _message == null ) {
                using ( StringBuilderCache w = StringBuilderCache.Acquire(  ) ) {
                    _formatter.AppendTo( w, CultureInfo.InvariantCulture, _values );
                    _message = w.ToString();
                }
            }
            return _message;
        }

    }
}
