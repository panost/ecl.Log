using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecl.Log.File;
using ecl.Log.Format;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ecl.Log {
    public static class LoggerExtensions {

        private static readonly Func<string, Exception, string> _messageFormatter
            = ( message, exception ) => message;

        public static void Log( this ILogger log, LogLevel logLevel, string message ) {
            log.Log( logLevel, default(EventId), message, null, _messageFormatter );
        }

        private static readonly Func<ValuesLog, Exception, string> _messageValuesFormatter
            = ( message, exception ) => message.ToString();

        public static void Log( this ILogger log, LogLevel logLevel, string message, params object[] args ) {
            ValuesLog l = ValuesLog.Get( message, args );
            if ( l == null ) {
                log.Log( logLevel, default(EventId), message, null, _messageFormatter );
            } else {
                log.Log( logLevel, default(EventId), l, null, _messageValuesFormatter );
            }
        }

        public static void Debug( this ILogger log, string message ) {
            Log( log, LogLevel.Debug, message );
        }
        public static void Debug( this ILogger log, string message, params object[] args ) {
            Log( log, LogLevel.Debug, message, args );
        }
        public static void Info( this ILogger log, string message ) {
            Log( log, LogLevel.Information, message );
        }
        public static void Info( this ILogger log, string message, params object[] args ) {
            Log( log, LogLevel.Information, message, args );
        }
        private static readonly Func<string, Exception, string> _errorFormatter
            = ( message, exception ) => exception.Message + ", " + message ;

        public static bool Error( this ILogger log, Exception error, string message = null ) {
            log.Log( LogLevel.Error, default, null, error, _errorFormatter );
            return false;
        }
    }
}