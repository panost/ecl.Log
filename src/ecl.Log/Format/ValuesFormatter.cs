using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ecl.Log.Format {
    [DebuggerDisplay( "Format: {Format}" )]
    public partial class ValuesFormatter : IReadOnlyList<string> {
        private static readonly ConcurrentDictionary<string, ValuesFormatter> _formatters
            = new ConcurrentDictionary<string, ValuesFormatter>( StringComparer.Ordinal );
        private static readonly ValuesFormatter Empty = new ValuesFormatter();
        public readonly int Hash;
        public readonly string Format;
        private Segment[] _segs;
        private string[] _names;

        private ValuesFormatter( string format = null ) {
            _segs = Array.Empty<Segment>();
            _names = Array.Empty<string>();
            Format = format;
        }

        public int Count => _names.Length;

        private ValuesFormatter( string format, Segment[] segs, string[] names ) {
            Hash = StringComparer.Ordinal.GetHashCode( format );
            Format = format;
            _segs = segs;
            _names = names;
        }
        public static ValuesFormatter Get( string format ) {
            if ( string.IsNullOrWhiteSpace( format ) ) {
                return Empty;
            }
            ValuesFormatter formatter;
            if ( !_formatters.TryGetValue( format, out formatter ) ) {

                if ( TryParse( format, out formatter ) ) {

                    formatter = _formatters.GetOrAdd( format, formatter );
                }
            }
            return formatter;
        }

        

        public IEnumerator<string> GetEnumerator() {
            return ( (IEnumerable<string>)_names ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public string this[ int index ] => _names[ index ];

        public override int GetHashCode() {
            return Hash;
        }

        private static readonly char[] _invalidCharacters = { '{', '}', ':', ',' };

        

        public override string ToString() {
            return Format;
        }

        public static bool TryParse( string format, out ValuesFormatter formatter ) {
            if ( string.IsNullOrEmpty( format ) ) {
                formatter = Empty;
                return true;
            }
            //if ( format.IndexOf( '{' ) < 0 ) {
            //    formatter = new ValuesFormatter( format );
            //    return true;
            //}
            Parser parser = default;
            if ( parser.TryParse( format ) ) {
                if ( parser.Count == 1 ) {
                    var seg = parser.Segments[ 0 ];
                    if ( seg.Type == SegmentType.Text && seg.Format == format ) {
                        formatter = new ValuesFormatter( format );
                        return true;
                    }
                }
                formatter = new ValuesFormatter( format, parser.Segments, parser.Names );
                return true;
            }
            formatter = null;
            return false;
        }
    }
}
