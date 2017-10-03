using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ecl.Log {
    public abstract class LoggerBase : ILogger {
        public virtual void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter ) {
            
            var pair = state as IReadOnlyList<KeyValuePair<string, object>>;

        }

        public abstract IDisposable BeginScope<TState>( TState state );
        public abstract bool IsEnabled( LogLevel logLevel );


    }
}
