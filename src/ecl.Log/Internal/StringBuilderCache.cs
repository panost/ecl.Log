using System;
using System.Text;
using System.Threading;

namespace ecl.Log {
    public struct StringBuilderCache : IDisposable {
        public readonly StringBuilder Builder;
        [ThreadStatic]
        private static StringBuilder _cachedInstance;
        private const int MAX_BUILDER_SIZE = 360;
        private StringBuilderCache( StringBuilder builder ) {
            Builder = builder;
        }
        public static StringBuilderCache Acquire( int capacity = 120 ) {
            if ( capacity <= MAX_BUILDER_SIZE ) {
                StringBuilder sb = _cachedInstance;
                if ( sb != null ) {
                    // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                    // when the requested size is larger than the current capacity
                    if ( capacity <= sb.Capacity ) {
                        _cachedInstance = null;
                        sb.Clear();
                        return new StringBuilderCache( sb );
                    }
                }
            }
            return new StringBuilderCache( new StringBuilder( capacity ) );
        }
        public static implicit operator StringBuilder( StringBuilderCache c ) {
            return c.Builder;
        }
        public void Dispose() {
            if ( Builder.Capacity <= MAX_BUILDER_SIZE ) {
                _cachedInstance = Builder;
            }
        }

        public override string ToString() {
            return Builder.ToString();
        }
    }
}
