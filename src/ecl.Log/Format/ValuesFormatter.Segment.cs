using System;
using System.Text;

namespace ecl.Log.Format {
    partial class ValuesFormatter {
        public enum Alignment : byte {
            Default,
            Left,
            Right,
            Center,
            None
        }
        public enum SegmentType : byte {
            Text,
            Default,
            String,
            Struct
        }
        struct Segment {
            public string Format;
            public short Padding;
            public short Index;
            public SegmentType Type;
            public Alignment Align;

            public Segment( string text ) {
                Format = text;
                Padding = 0;
                Index = 0;
                Type = SegmentType.Text;
                Align = Alignment.Default;
            }

            
        }
    }
}