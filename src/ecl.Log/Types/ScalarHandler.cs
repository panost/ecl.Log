using ecl.Log.Formatters;
using Microsoft.Extensions.Logging;
using System;
namespace ecl.Log.Types {
    class ScalarHandler<T> : TypeHandler<T> {

        class ScalarScope : Scope {
            readonly T _state;

            public ScalarScope( Logger logger, T state )
                : base( logger ) {
                _state = state;
            }

            protected override void AppendScope( LoggerFormatter formatter ) {
                formatter.BeginScalar();
                ( (ScalarHandler<T>)formatter.Handler ).AppendValue( formatter.Builder, _state );
                formatter.CloseScalar();
            }
        }

        public override IDisposable CreateScope( Logger logger, T state ) {
            return new ScalarScope( logger, state );
        }

        public override void Log( LoggerFormatter f, LogLevel logLevel, EventId eventId,
            T state, Exception exception, Func<T, Exception, string> formatter ) {
            f.AppendHead( logLevel, eventId, formatter( state, exception ) );
        }
    }
}
