using System;
using System.Data;
using System.Globalization;
using System.Text;
using ecl.Log.Formatters;
using Microsoft.Extensions.Logging;

namespace ecl.Log.Types {
    class FormattableHandler<T> : ScalarHandler<T> where T : IConvertible, IFormattable {
        private string _format;

        /// <summary>
        /// 
        /// </summary>
        public string Format {
            get {
                return _format;
            }
            set {
                _format = value;
            }
        }

        public override void AppendValue( StringBuilder b, T value ) {
            if ( value != null ) {
                b.Append( value.ToString( _format, CultureInfo.InvariantCulture ) );
            }
        }
    }

    class Int32Handler : ScalarHandler<int> {
        public override void AppendValue( StringBuilder b, int value ) {
            b.Append( value );
        }
    }
    class StringHandler : TypeHandler<string> {
        
        public override void AppendValue( StringBuilder b, string value ) {
            b.Append( value );
        }

        public override IDisposable CreateScope( Logger logger, string state ) {
            if ( string.IsNullOrEmpty( state ) ) {
                return Scope.Empty;
            }
            return new StringScope( logger, state );
        }

        public override void Log( LoggerFormatter f, LogLevel logLevel, EventId eventId, string state, Exception exception, Func<string, Exception, string> formatter ) {
            if ( string.IsNullOrEmpty( state ) ) {
                if ( exception != null ) {
                    f.AppendHead( logLevel, eventId, exception.Message );
                    f.AppendException( exception );
                }
                return;
            }
            f.AppendHead( logLevel, eventId, state );
            if ( exception != null ) {
                f.AppendException( exception );
            }
        }
    }
}
