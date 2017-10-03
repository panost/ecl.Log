using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using ecl.Log.Formatters;
using ecl.Log.Types;

namespace ecl.Log {
    public class Logger : ILogger {
        public readonly LoggerProvider Provider;
        public string Name {
            get;
        }
        public Logger( LoggerProvider provider, string name ) {
            Provider = provider;
            Name = name;
        }
        public bool IncludeScopes {
            get; set;
        }
        private Func<string, LogLevel, bool> _filter;
        /// <summary>
        /// 
        /// </summary>
        public Func<string, LogLevel, bool> Filter {
            get {
                return _filter;
            }
            set {
                _filter = value;
            }
        }

        private Func<LoggerFormatter> _formater;
        public void Log<TState>( LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter ) {
            if ( !IsEnabled( logLevel ) ) {
                return;
            }
            LoggerFormatter fmt = _formater?.Invoke();
            if ( fmt == null ) {
                fmt = LoggerFormatter.Get( this, Provider.GetSettings()?.Formatter, out _formater );
            }

            using ( var c = StringBuilderCache.Acquire() ) {
                var handler = TypeHandler<TState>.Default;
                fmt.Init( c, handler );
                handler.Log( fmt, logLevel, eventId, state, exception, formatter );
                if ( IncludeScopes ) {
                    fmt.AppendScopes( _current );
                }
                fmt.Flush();
            }
        }

        
        public bool IsEnabled( LogLevel logLevel ) {
            if ( (uint)logLevel < (uint)LogLevel.None ) {
                if ( _filter != null ) {
                    return _filter( Name, logLevel );
                }
                return true;
            }
            return false;
        }

        internal Scope _current;

        //internal Scope[] GetScopes() {
        //    return Scope.GetAll( _current );
        //}

        IDisposable ILogger.BeginScope<TState>( TState state ) {
            if ( state == null ) {
                return Scope.Empty;
            }
            return TypeHandler<TState>.Default.CreateScope( this, state );
        }

        protected internal virtual void ApplySettings( LoggerSettings options ) {
            _formater = null;
        }

        
    }
}
