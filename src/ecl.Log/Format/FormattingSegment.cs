using System;
using System.Text;

namespace ecl.Log.Format {
    public struct FormattingSegment {
        public string Format;
        public short Padding;
        public short Index;
        public SegmentType Type;
        public Alignment Align;

        public FormattingSegment( string text ) {
            Format = text;
            Padding = 0;
            Index = 0;
            Type = SegmentType.Text;
            Align = Alignment.Default;
        }

        public void AppendTo( StringBuilder b, string text, char ch = ' ' ) {
            if ( Align == Alignment.Default ) {
                if ( text != null )
                    b.Append( text );
                return;
            }
            if ( string.IsNullOrEmpty( text ) ) {
                b.Append( ch, Padding );
                return;
            }
            int remain = Padding - text.Length;
            if ( remain <= 0 ) {
                b.Append( text );
                return;
            }
            switch ( Align ) {
            case Alignment.Left:
                b.Append( ch, remain );
                b.Append( text );
                return;
            case Alignment.Right:
                b.Append( text );
                b.Append( ch, remain );
                return;
            }
            remain /= 2;
            b.Append( ch, remain / 2 );
            b.Append( text );
            b.Append( ch, remain - remain/2 );
        }
    }
}
