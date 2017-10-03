using System;
using Microsoft.Extensions.Logging;

namespace ecl.Log {
    public static class LogLevelUtil {
        public static readonly Func<string, LogLevel, bool> True = ( cat, level ) => true;
        public static readonly Func<string, LogLevel, bool> False = ( cat, level ) => false;

        private static readonly Func<string, LogLevel, bool>[] _filters 
            = new Func<string, LogLevel, bool>[ 5 ];

        public static Func<string, LogLevel, bool> GetFilter( this LogLevel level ) {
            uint idx = (uint)level;
            if ( idx < 6u ) {
                if ( idx == 0 )
                    return True;
                idx--;
                return _filters[ idx ]
                       ?? ( _filters[ idx ] = ( _, l ) => l >= level );
            }
            return False;
        }
        public static Func<string, LogLevel, bool> GetFilter( this LogLevel level, string name ) {
            if ( string.IsNullOrEmpty( name ) ) {
                return level.GetFilter();
            }
            if ( (uint)level <= (uint)LogLevel.Critical ) {
                if ( level == LogLevel.Trace ) {
                    return ( n, l ) => string.Equals( n, name, StringComparison.OrdinalIgnoreCase );
                }
                return ( n, l ) => l >= level && string.Equals( n, name, StringComparison.OrdinalIgnoreCase );
            }
            return False;
        }
        private static readonly string[] _shortCuts = {
            "trce", // Trace
            "dbug", // Debug
            "info", //Information
            "warn", //Warning
            "fail", //Error
            "crit" //Critical
        };

        public static string ToShortCut( this LogLevel level ) {
            if ( (uint)level < 6u ) {
                return _shortCuts[ (uint)level ];
            }
            throw new ArgumentOutOfRangeException( nameof( level ) );
        }
    }
}
