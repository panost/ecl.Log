using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ecl.Log.Format;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ecl.Log {
    internal static class Util {
        public static T GetEnum<T>( this IConfiguration configuration,
            string key, T defaultValue ) where T:struct {
            var str = configuration?[ key ];
            //T temp;
            if ( !string.IsNullOrEmpty( str ) ) {
                if ( Enum.TryParse( str, true, out T temp )
                     && Enum.IsDefined( typeof( T ), temp ) ) {
                    return temp;
                }
            }
            return defaultValue;
        }
        public static int GetInt32( this IConfiguration configuration,
            string key, int defaultValue = 0 ) {
            var str = configuration?[ key ];
            //T temp;
            if ( !string.IsNullOrEmpty( str ) ) {
                if ( int.TryParse( str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int temp ) ) {
                    return temp;
                }
            }
            return defaultValue;
        }
        public static bool GetBoolean( this IConfiguration configuration,
            string key, bool defaultValue = false ) {
            var str = configuration?[ key ];
            //T temp;
            if ( !string.IsNullOrEmpty( str ) ) {
                if ( bool.TryParse( str, out bool temp ) ) {
                    return temp;
                }
            }
            return defaultValue;
        }
        private static string ListToString(IReadOnlyList<KeyValuePair<string, object>> list,
            Exception error) {
            using ( StringBuilderCache c = StringBuilderCache.Acquire() ) {
                StringBuilder b = c;
                string delim = "";
                foreach ( var entry in list ) {
                    b.Append( delim );
                    b.Append( entry.Key );
                    b.Append( ':' );
                    b.Append( entry.Value );
                    delim = ",";
                }
                return c.ToString();
            }
        }

        private static readonly Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string> _keyValuesFormatter
            = ListToString;

        public static void Method( this ILogger log, LogLevel logLevel, string message, [CallerMemberName]string name = null ) {
            KeyValuePair<string, object>[] val = {
                new KeyValuePair<string, object>( "Method", name ),
                new KeyValuePair<string, object>( "Message", message ),
            };
            log.Log( logLevel, default( EventId ), val, null, _keyValuesFormatter );
        }

        internal static bool TryGetSwitch<T>( this IDictionary<string, T> map, string name, out T value ) {
            while ( !string.IsNullOrEmpty( name ) ) {
                if ( map.TryGetValue( name, out value ) ) {
                    return true;
                }
                int idx = name.LastIndexOf( '.' );
                if ( idx <= 0 ) {
                    // "." is treated as "Default"
                    break;
                }
                name = name.Substring( 0, idx );
            }
            return map.TryGetValue( "Default", out value );
        }
    }
}