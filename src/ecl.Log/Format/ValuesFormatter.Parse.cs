using System;
using System.Collections.Generic;
using System.Text;

namespace ecl.Log.Format {
    partial class ValuesFormatter {
        struct MessageTemplateParser {
            private string _message;
            private int _index;
            private int _length;
            private StringBuilder _rawBuffer;
            private List<FormattingSegment> list;
            private List<string> names;

            private int _maxIndex;
            private int? _number;
            private SegmentType _mode;
            private int? _paddingSize;
            private string _format;
            private string _name;
            private Alignment _alignMode;

            public int Count {
                get {
                    return list.Count;
                }
            }
            public int Arguments {
                get {
                    return Math.Max( _maxIndex+1, names.Count );
                }
            }
            public FormattingSegment[] Segments {
                get {
                    return list.ToArray();
                }
            }

            public string[] Names {
                get {
                    return names.ToArray();
                }
            }

            private void EnsureValidIndex( int index ) {
                while ( index >= names.Count ) {
                    names.Add( null );
                }
            }
            private bool AddSegment() {
                int segIndex;
                if ( string.IsNullOrEmpty( _name ) ) {
                    if ( !_number.HasValue ) {
                        return false;
                    }
                    segIndex = _number.Value;
                    if ( segIndex >= names.Count && names.Count > 0 ) {
                        return false;
                    }
                    //_name = names[ _number.Value ];
                } else {
                    int nameIndex = names.IndexOf( _name );
                    if ( nameIndex >= 0 ) {
                        segIndex = nameIndex;
                    } else if ( _number.HasValue ) {
                        // name and index specified and name doesn't already exist
                        segIndex = _number.Value;
                        if ( segIndex < names.Count ) {
                            // index points to an old position
                            if ( null != names[ segIndex ] ) {
                                return false;
                            }
                            names[ segIndex ] = _name;
                        } else {
                            EnsureValidIndex( segIndex );
                            names[ segIndex ] = _name;
                        }
                    } else {
                        // only name exists
                        segIndex = AddName();
                    }
                }
                if ( segIndex > _maxIndex ) {
                    _maxIndex = segIndex;
                }
                if ( _alignMode != Alignment.None ) {
                    FormattingSegment seg;
                    seg.Format = _format;
                    seg.Index = (short)segIndex;
                    seg.Type = _mode;
                    seg.Align = _alignMode;
                    if ( _alignMode == Alignment.Default ) {
                        seg.Padding = 0;
                    } else {
                        seg.Padding = (short)_paddingSize.GetValueOrDefault();
                    }
                    list.Add( seg );
                }
                return true;
            }

            private int AddName() {
                for ( int i = 0; i < names.Count; i++ ) {
                    if ( names[ i ] == null ) {
                        names[ i ] = _name;
                        return i;
                    }
                }
                names.Add( _name );
                return names.Count - 1;
            }

            public bool TryParse( string message ) {
                _message = message;
                _index = 0;
                _length = message.Length;
                list = new List<FormattingSegment>();
                names = new List<string>();
                _rawBuffer = new StringBuilder();
                _maxIndex = -1;
                while ( AppendUntil( '{', true ) ) {
                    int bookmark = ++_index;
                    string segUpNow = _rawBuffer.ToString();
                    if ( !ParseSegment() ) {
                        _index = bookmark;
                        _rawBuffer.Clear();
                        _rawBuffer.Append( segUpNow );
                        _rawBuffer.Append( '{' );
                        continue;
                    }
                    _rawBuffer.Clear();

                    if ( segUpNow.Length > 0 ) {
                        list.Add( new FormattingSegment( segUpNow ) );
                    }
                    if ( !AddSegment() ) {
                        return false;
                    }
                }
                if ( _rawBuffer.Length > 0 ) {
                    list.Add( new FormattingSegment( _rawBuffer.ToString() ) );
                }
                return true;
            }

            bool SkipWhiteSpace() {
                while ( _index < _length ) {
                    if ( !char.IsWhiteSpace( _message, _index ) )
                        return true;
                    _index++;
                }
                return false;
            }


            private bool ParseMode() {
                _mode = SegmentType.Default;
                if ( !SkipWhiteSpace() ) {
                    return false;
                }
                char ch = _message[ _index ];
                switch ( ch ) {
                case '@':
                    _mode = SegmentType.Struct;
                    _index++;
                    break;
                case '$':
                    _mode = SegmentType.String;
                    _index++;
                    break;
                default:
                    return true;
                }
                return SkipWhiteSpace();
            }


            private bool ParseIndex() {
                return ParseInteger( false, out _number );
            }

            private bool ParseInteger( bool isAlign, out int? number ) {
                number = null;
                if ( !SkipWhiteSpace() ) {
                    return false;
                }
                int digits = 0;
                int num = 0;
                if ( isAlign && _index < _length ) {
                    switch ( _message[ _index ] ) {
                    case '-':
                        _alignMode = Alignment.Right;
                        goto case '+';
                    case '^':
                        _alignMode = Alignment.Center;
                        goto case '+';
                    case '+':
                        _index++;
                        if ( !SkipWhiteSpace() ) {
                            return false;
                        }
                        break;

                    }
                }
                while ( _index < _length ) {
                    char ch = _message[ _index ];
                    int digit = ch - '0';
                    if ( digit < 0 || digit > 9 ) {
                        break;
                    }
                    num = num * 10 + digit;
                    _index++;
                    digits++;
                }
                if ( digits > 0 ) {
                    if ( isAlign ) {
                        if ( num > 0 ) {
                            if ( _alignMode == Alignment.Default ) {
                                _alignMode = Alignment.Left;
                            }
                        } else {
                            _alignMode = Alignment.Default;
                        }
                    }

                    number = num;
                } else if ( isAlign ) {
                    return false;
                }
                return SkipWhiteSpace();
            }

            private static bool IsStartNameChar( char ch ) {
                return char.IsLetter( ch ) || ch == '_';
            }
            //private static bool IsNameChar( char ch ) {
            //    return char.IsLetterOrDigit( ch )
            //        || ch == '.'
            //        || ch == '-'
            //        || ch == '_';
            //}
            private static bool IsNameChar( char ch ) {
                switch ( ch ) {
                case ':':
                case ',':
                case '}':
                    return false;
                }
                return true;
            }
            private void ParseName() {
                int startIndex = _index;
                while ( _index < _length ) {
                    char ch = _message[ _index ];

                    if ( !IsNameChar( ch ) ) {
                        break;
                    }
                    _index++;
                }
                Unescape( startIndex, _index - startIndex, '}' );
            }
            private bool ParseSegment() {
                _paddingSize = null;
                _format = null;
                _name = null;
                _rawBuffer.Clear();
                _name = null;
                if ( !ParseMode() || !ParseIndex() ) {
                    return false;
                }
                const int StateInit = 0;
                const int StateName = 1;
                const int StatePad = 2;
                const int StateFormat = 3;
                int state = StateInit;

                while ( SkipWhiteSpace() ) {
                    char ch = _message[ _index ];

                    switch ( ch ) {
                    case '}':
                        _index++;
                        return true;
                    case ':':
                        _index++;
                        if ( state >= StateFormat || !ParseFormat() ) {
                            return false;
                        }
                        state = StateFormat;
                        _format = _rawBuffer.ToString();
                        break;
                    case ',':
                        if ( state >= StatePad || _paddingSize.HasValue ) {
                            return false;
                        }
                        _index++;
                        if ( !ParseAligment() ) {
                            return false;
                        }
                        state = StatePad;
                        break;
                    case '\'':
                        if ( state >= StateName || !AppendQuoted() ) {
                            return false;
                        }
                        _name = _rawBuffer.ToString();
                        _rawBuffer.Clear();
                        state = StateName;
                        break;
                    default:
                        if ( state >= StateName || !IsStartNameChar( ch ) ) {
                            return false;
                        }
                        _rawBuffer.Append( ch );
                        _index++;
                        ParseName();
                        _name = _rawBuffer.ToString();
                        break;
                    }
                }
                return false;
            }

            

            private bool ParseAligment() {
                return ParseInteger( true, out _paddingSize );
            }

            private bool AppendQuoted() {
                _rawBuffer.Clear();
                if ( _index >= _length )
                    return false;
                char ch = _message[ _index ];
                _index++;
                if ( AppendUntil( ch, false ) ) {
                    _index++;
                    return SkipWhiteSpace();
                }
                return false;
            }

            private bool ParseFormat() {
                _rawBuffer.Clear();
                if ( _index >= _length )
                    return false;
                return AppendUntil( '}', false );
            }

            private void Unescape( int startIndex, int length, char escapeCharacter ) {
                if ( escapeCharacter == '{' ) {
                    escapeCharacter = '}';
                } else if ( escapeCharacter == '}' ) {
                    escapeCharacter = '{';
                } else {
                    _rawBuffer.Append( _message, startIndex, length );
                    return;
                }
                while ( length > 0 ) {
                    int index = _message.IndexOf( escapeCharacter, startIndex, length );
                    if ( index >= 0 ) {
                        int textlength = index - startIndex;
                        _rawBuffer.Append( _message, startIndex, textlength );
                        startIndex += textlength;
                        _rawBuffer.Append( escapeCharacter );
                        if ( index + 1 < length ) {
                            if ( _message[ index + 1 ] == escapeCharacter ) {
                                startIndex += 2;
                                length -= 2;
                            }
                        } else {
                            return;
                        }
                        length -= textlength;
                    } else {
                        break;
                    }
                }
                //length -= startIndex;
                if ( length > 0 ) {
                    _rawBuffer.Append( _message, startIndex, length );
                }
            }

            private bool AppendUntil( char stopCharacter, bool flush ) {
                while ( _index < _length ) {
                    int index = _message.IndexOf( stopCharacter, _index );
                    if ( index >= 0 ) {
                        int textlength = index - _index;
                        if ( textlength > 0 ) {
                            Unescape( _index, textlength, stopCharacter );
                        }
                        _index += textlength;
                        if ( index + 1 < _length ) {
                            if ( _message[ index + 1 ] == stopCharacter ) {
                                _rawBuffer.Append( stopCharacter );
                                _index += 2;
                            } else {
                                return true;
                            }
                        } else {
                            if ( flush ) {
                                _rawBuffer.Append( stopCharacter );
                            }
                            return true;
                        }
                    } else {
                        Unescape( _index, _length - _index, stopCharacter );
                        _index = _length;
                    }
                }
                return false;
            }

        }

    }
}
