using System;
using Microsoft.Extensions.Logging;

namespace ecl.Log {
    public struct LogEvent<TState> {
        public Exception Exception;
        public Func<TState, Exception, string> Formatter;
        public LogLevel Level;
        public DateTime TimeStamp;
        public EventId EventId;
        public TState State;

        public LogEvent( LogLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter ) {
            Level = level;
            EventId = eventId;
            State = state;
            Exception = exception;
            Formatter = formatter;
            TimeStamp = DateTime.UtcNow;
        }
    }
}
