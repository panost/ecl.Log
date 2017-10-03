using System;
using System.Collections.Generic;
using System.Text;
using ecl.Log.Formatters;
using Microsoft.Extensions.Logging;

namespace ecl.Log.Types {
    class KeyValueHandler<T> : TypeHandler<T> 
        where T: IReadOnlyList<KeyValuePair<string, object>> {

        private void AppendProperties( LoggerFormatter formatter, T state, bool scoped = false ) {
            bool propertyFound = false;
            int count = state.Count;
            for ( int i = 0; i < count; ) {
                var pair = state[ i ];
                i++;
                if ( i == count ) {
                    // last property
                    if ( pair.Key == "{OriginalFormat}" ) {
                        break;
                    }
                }
                if ( !propertyFound ) {
                    if ( scoped )
                    formatter.BeginScope();
                    formatter.BeginProperties();
                    propertyFound = true;
                }
                formatter.AppendProperty( pair.Key, pair.Value );
            }
            if ( propertyFound ) {
                formatter.CloseProperties();
                if ( scoped )
                formatter.CloseScope();
            }
        }
        class KeyValueScope : Scope {
            readonly T _state;

            public KeyValueScope( Logger logger, T state )
                : base( logger ) {
                _state = state;
            }

            protected override void AppendScope( LoggerFormatter formatter ) {
                ( (KeyValueHandler<T>)formatter.Handler )
                    .AppendProperties( formatter, _state, true );
            }
        }
        public override IDisposable CreateScope( Logger logger, T state ) {
            return new KeyValueScope( logger, state );
        }

        public override void Log( LoggerFormatter f, LogLevel logLevel, EventId eventId, 
            T state, Exception exception, Func<T, Exception, string> formatter ) {
            f.AppendHead( logLevel, eventId, formatter( state, exception ) );
            if ( exception != null ) {
                f.AppendException( exception );
            }
            AppendProperties( f, state );
        }

    }
}
