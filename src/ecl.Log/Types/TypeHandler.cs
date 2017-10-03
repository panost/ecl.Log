using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ecl.Log.Formatters;
using Microsoft.Extensions.Logging;

namespace ecl.Log.Types {
    public abstract class TypeHandler {
        /// <summary>
        /// The supported type
        /// </summary>
        public Type Type { get; private set; }

        private static readonly ConcurrentDictionary<Type,TypeHandler> 
            _handlers = new ConcurrentDictionary<Type, TypeHandler>();

        public static TypeHandler Get( Type type ) {
            TypeHandler handler;
            if ( !_handlers.TryGetValue( type, out handler ) ) {
                handler = _handlers.GetOrAdd( type, Create( type ) );
            }
            return handler;
        }

        protected static TypeHandler Create( Type type ) {
            var code = Type.GetTypeCode( type );
            Type genType = null;

            switch ( code ) {
            case TypeCode.Object:
                genType = GetObjectHandler( type );
                break;
            case TypeCode.Int32:
                return new Int32Handler();
            case TypeCode.DateTime:
                return new FormattableHandler<DateTime>() {
                    Format = "s"
                };

            case TypeCode.Decimal:
                return new FormattableHandler<decimal>() {
                    Format = "g"
                };
            case TypeCode.Double:
                return new FormattableHandler<double>() {
                    Format = "r"
                };
            case TypeCode.Single:
                return new FormattableHandler<float>() {
                    Format = "r"
                };
            case TypeCode.Boolean:
            case TypeCode.Char:
                break;
            case TypeCode.String:
                return new StringHandler();
            default:
                if ( code >= TypeCode.SByte && code <= TypeCode.UInt64 ) {
                    genType = typeof( FormattableHandler<> );
                }
                break;
            }

            if ( genType == null ) {
                genType = typeof( ScalarHandler<> );
            }
            var handler = (TypeHandler)Activator.CreateInstance( genType.MakeGenericType( type ) );
            handler.Type = type;
            return handler;
        }

        private static Type GetObjectHandler( Type type ) {
            if ( typeof( IReadOnlyList<KeyValuePair<string, object>> ).IsAssignableFrom( type ) ) {
                return typeof( KeyValueHandler<> );
            }
            if ( type == typeof( object ) || type.IsInterface ) {

            }
            return null;
        }
        

    }

    public abstract class TypeHandler<T> : TypeHandler {
        private static TypeHandler<T> _default;
        /// <summary>
        /// 
        /// </summary>
        public static TypeHandler<T> Default {
            get {
                if ( _default == null ) {
                    _default = (TypeHandler<T>)Get( typeof( T ) );
                }
                return _default;
            }
            internal set {
                _default = value;
            }
        }
        public virtual void AppendValue( StringBuilder b, T value ) {
            if ( value != null ) {
                b.Append( value.ToString() );
            }
        }
        public abstract IDisposable CreateScope( Logger logger, T state );
        public abstract void Log( LoggerFormatter f, LogLevel logLevel, EventId eventId,
            T state, Exception exception, Func<T, Exception, string> formatter );
    }

}
