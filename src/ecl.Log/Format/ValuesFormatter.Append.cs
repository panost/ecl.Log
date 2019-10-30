using System;
using System.Collections.Generic;
using System.Text;

namespace ecl.Log.Format {
    partial class ValuesFormatter {

        public void AppendTo<M>( StringBuilder b, IFormatProvider provider, M args )
            where M : IReadOnlyList<object> {

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
            foreach ( Segment seg in _segs ) {
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
                AppendTo( seg, b, str );
            }
        }

        private void AppendTo( Segment seg, StringBuilder b, string text, char ch = ' ' ) {
            if ( seg.Align == Alignment.Default ) {
                if ( text != null )
                    b.Append( text );
                return;
            }
            if ( string.IsNullOrEmpty( text ) ) {
                b.Append( ch, seg.Padding );
                return;
            }
            int remain = seg.Padding - text.Length;
            if ( remain <= 0 ) {
                b.Append( text );
                return;
            }
            switch ( seg.Align ) {
            case Alignment.Left:
                b.Append( ch, remain );
                b.Append( text );
                return;
            case Alignment.Right:
                b.Append( text );
                b.Append( ch, remain );
                return;
            }
            b.Append( ch, remain / 2 );
            b.Append( text );
            b.Append( ch, remain - remain / 2 );
        }
    }
}
