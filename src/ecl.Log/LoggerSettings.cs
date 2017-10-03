using System;
using System.Collections.Generic;
using ecl.Log.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace ecl.Log {
    public class LoggerSettings {
        public readonly IConfiguration Configuration;

        protected LoggerSettings( IConfiguration configuration ) {
            Configuration = configuration;
            _changeToken = configuration.GetReloadToken();
        }

        protected LoggerSettings() {
        }
        private IChangeToken _changeToken;
        /// <summary>
        /// 
        /// </summary>
        public IChangeToken ChangeToken => _changeToken;


        private byte _includeScopes;
        public bool IncludeScopes {
            get {
                if ( _includeScopes == 0 ) {
                    _includeScopes = Configuration.GetBoolean( "IncludeScopes" )
                        ? (byte)1
                        : (byte)2;
                }
                return _includeScopes == 1;
            }
            set => _includeScopes = value ? (byte)1 : (byte)2;
        }
        private FormatterSettings _formatter;
        /// <summary>
        /// 
        /// </summary>
        public FormatterSettings Formatter {
            get {
                if ( _formatter == null ) {
                    if ( Configuration != null ) {
                        _formatter = new FormatterSettings( Configuration.GetSection( "Formatter" ) );
                    } else {
                        _formatter = new FormatterSettings( null );
                    }
                }
                return _formatter;
            }
            set {
                _formatter = value;
            }
        }
        private LogLevel _minLevel = LogLevel.None;
        /// <summary>
        /// 
        /// </summary>
        public LogLevel MinLevel {
            get {
                if ( _minLevel == LogLevel.None ) {
                    _minLevel = Configuration.GetEnum( "MinLevel", LogLevel.Trace );
                }
                return _minLevel;
            }
            set {
                _minLevel = value;
            }
        }

        private Dictionary<string, LogLevel> _switches = new Dictionary<string, LogLevel>();
        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, LogLevel> Switches => _switches;

        public virtual bool TryGetSwitch( string name, out LogLevel level ) {
            if ( !_switches.TryGetSwitch( name, out level )
                && Configuration != null ) {
                IConfigurationSection switches = Configuration.GetSection( "LogLevel" );
                if ( switches == null ) {
                    level = LogLevel.None;
                    return false;
                }

                var value = switches[ name ];
                if ( string.IsNullOrEmpty( value ) ) {
                    level = LogLevel.None;
                    return false;
                }
                if ( !Enum.TryParse( value, true, out level ) ) {
                    var message = $"Configuration value '{value}' for category '{name}' is not supported.";
                    throw new InvalidOperationException( message );
                }
                _switches.Add( name, level );
            }
            return true;
        }
        protected virtual void Reloaded() {
            _changeToken = null;
            _switches = null;
        }

        
    }
}
