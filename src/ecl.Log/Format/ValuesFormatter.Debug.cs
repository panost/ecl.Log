using System;
using System.Text;

#if DEBUG

namespace ecl.Log.Format {
    partial class ValuesFormatter {
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
                AppendTo( seg, b, name );
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
    }
}

#endif
