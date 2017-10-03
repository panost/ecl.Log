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
        private FormattingSegment[] _segs;
        private string[] _names;

        private ValuesFormatter( string format = null ) {
            _segs = Array.Empty<FormattingSegment>();
            _names = Array.Empty<string>();
            Format = format;
        }

        public int Count => _names.Length;

        private ValuesFormatter( string format, FormattingSegment[] segs, string[] names ) {
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

        public void AppendTo<M>( StringBuilder b, IFormatProvider provider, M args )
            where M: IReadOnlyList<object> {
            if ( _segs.Length == 0 ) {
                if ( Format != null ) {
                    b.Append( Format );
                }
                return;
            }
            int argLength = args != null ? args.Count : 0;
            ICustomFormatter cf = null;
            if ( provider != null ) {
                cf = (ICustomFormatter)provider.GetFormat( typeof( ICustomFormatter ) );
            }
            foreach ( FormattingSegment seg in _segs ) {
                if ( seg.Type == SegmentType.Text ) {
                    b.Append( seg.Format );
                    continue;
                }
                string str;
                if ( seg.Index < argLength ) {
                    object value = args[ seg.Index ];
                    str = cf?.Format( seg.Format, value, provider );
                    if ( str == null ) {
                        IFormattable fmta = value as IFormattable;
                        if ( fmta != null ) {
                            str = fmta.ToString( seg.Format, provider );
                        } else if ( value != null ) {
                            str = value.ToString();
                        }
                    }
                    if ( str == null )
                        str = "";
                } else {
                    str = "";
                }
                seg.AppendTo( b, str );
            }
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

        public void AppendFormat( StringBuilder b ) {
            if ( _segs.Length == 0 ) {
                b.Append( Format );
                return;
            }
            for ( int i = 0; i < _segs.Length; i++ ) {
                var seg = _segs[ i ];
                if ( seg.Type == SegmentType.Text ) {
                    b.Append( seg.Format );
                    continue;
                }
                b.Append( '{' );
                string name = seg.Index < _names.Length ? _names[ seg.Index ] : null;
                if ( !string.IsNullOrEmpty( name ) ) {
                    if ( name.IndexOfAny( _invalidCharacters ) >= 0 ) {
                        b.Append( '\'' );
                        b.Append( name.Replace( "'", "''" ) );
                        b.Append( '\'' );
                    } else {
                        b.Append( name );
                    }
                } else {
                    b.Append( seg.Index );
                }
                if ( seg.Align != Alignment.Default ) {
                    b.Append( ',' );
                    switch ( seg.Align ) {
                    case Alignment.Center:
                        b.Append( '^' );
                        break;
                    case Alignment.Left:
                        b.Append( '+' );
                        break;
                    case Alignment.Right:
                        b.Append( '-' );
                        break;
                    }
                }
                if ( !string.IsNullOrEmpty( seg.Format ) ) {
                    b.Append( ':' );
                    if ( seg.Format.IndexOfAny( _invalidCharacters ) >= 0 ) {
                        b.Append( '\'' );
                        b.Append( seg.Format.Replace( "'", "''" ) );
                        b.Append( '\'' );
                    } else {
                        b.Append( seg.Format );
                    }
                }
                b.Append( '}' );
            }
        }
        public void AppendNamedFormat( StringBuilder b ) {
            if ( _segs.Length == 0 ) {
                return;
            }
            for ( int i = 0; i < _segs.Length; i++ ) {
                var seg = _segs[ i ];
                if ( seg.Type == SegmentType.Text ) {
                    b.Append( seg.Format );
                    continue;
                }
                string name = seg.Index < _names.Length ? _names[ seg.Index ] : null;
                if ( string.IsNullOrEmpty( name ) ) {
                    name = ":Arg" + seg.Index;
                } else {
                    name = ':' + name;
                }
                seg.AppendTo( b, name );
            }
        }
        public void AppendNameIndexes( StringBuilder b ) {
            if ( _names.Length == 0 ) {
                return;
            }
            string delim = "[ ";
            for ( int i = 0; i < _names.Length; i++ ) {
                string name = _names[ i ];
                b.Append( delim );
                delim = ", ";
                if ( string.IsNullOrEmpty( name ) ) {
                    b.Append( "null" );
                } else {
                    b.Append( "'" );
                    b.Append( name );
                    b.Append( "'" );

                }
            }
            if ( delim == ", " ) {
                b.Append( " ]" );
            }
        }
        public void AppendJoinFormats( StringBuilder b ) {
            if ( _segs.Length == 0 ) {
                return;
            }
            string delim = "[ '";
            for ( int i = 0; i < _segs.Length; i++ ) {
                var seg = _segs[ i ];
                if ( seg.Type == SegmentType.Text || string.IsNullOrEmpty( seg.Format ) ) {
                    continue;
                }
                b.Append( delim );
                delim = "', '";
                b.Append( seg.Format );
            }
            if ( delim == "', '" ) {
                b.Append( "' ]" );
            }
        }
        public void AppendJoinPads( StringBuilder b ) {
            if ( _segs.Length == 0 ) {
                return;
            }
            string delim = "[ ";
            for ( int i = 0; i < _segs.Length; i++ ) {
                var seg = _segs[ i ];
                if ( seg.Align == Alignment.Default ) {
                    continue;
                }
                b.Append( delim );
                delim = ", ";
                switch ( seg.Align ) {
                case Alignment.Center:
                    b.Append( '^' );
                    break;
                case Alignment.Left:
                    b.Append( '+' );
                    break;
                case Alignment.Right:
                    b.Append( '-' );
                    break;
                }
                b.Append( seg.Padding );
            }
            if ( delim == ", " ) {
                b.Append( " ]" );
            }
        }
        public string ToString( char format ) {
            if ( format != 'F' && _segs.Length > 0 ) {
                using ( var b = StringBuilderCache.Acquire() ) {
                    if ( format == 'N' ) {
                        AppendNamedFormat( b );
                    } else if ( format == 'f' ) {
                        AppendJoinFormats( b );
                    } else if ( format == 'p' ) {
                        AppendJoinPads( b );
                    } else if ( format == 'i' ) {
                        AppendNameIndexes( b );
                    } else {
                        AppendFormat( b );
                    }
                    return b.ToString();
                }

            }
            return Format;
        }

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
            MessageTemplateParser parser = default;
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
