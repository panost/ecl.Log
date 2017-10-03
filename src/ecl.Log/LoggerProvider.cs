using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ecl.Log {
    public abstract class LoggerProvider : ILoggerProvider {
        private readonly Dictionary<string, Logger> _loggers
            = new Dictionary<string, Logger>( StringComparer.OrdinalIgnoreCase );

        private readonly Func<string, LogLevel, bool> _filter;
        private bool _includeScopes;
        protected IDisposable _optionsReloadToken;

        protected LoggerProvider() {
        }

        protected LoggerProvider( LoggerSettings settings ) {
            if ( settings == null ) {
                throw new ArgumentNullException( nameof(settings) );
            }
            settings.ChangeToken?.RegisterChangeCallback( OnConfigurationReload, null );
        }

        protected internal abstract LoggerSettings GetSettings( bool reload = false );
        protected internal abstract void Write( StringBuilder b );

        ILogger ILoggerProvider.CreateLogger( string categoryName ) {
            lock ( _loggers ) {
                if ( !_loggers.TryGetValue( categoryName, out var logger ) ) {
                    logger = CreateLogger( categoryName );
                    if ( logger != null ) {
                        var settings = GetSettings();
                        logger.Filter = GetFilter( categoryName, settings );
                        logger.IncludeScopes = _includeScopes;
                        logger.ApplySettings( settings );
                        _loggers.Add( categoryName, logger );
                    }
                }
                return logger;
            }
        }


        protected void ReloadLoggerOptions( LoggerSettings settings ) {
            _includeScopes = settings.IncludeScopes;
            foreach ( var logger in Loggers ) {
                logger.Filter = GetFilter( logger.Name, settings );
                logger.IncludeScopes = _includeScopes;
                logger.ApplySettings( settings );
            }
        }

        private void OnConfigurationReload( object state ) {
            LoggerSettings settings = null;
            try {
                // The settings object needs to change here, because the old one is probably holding on
                // to an old change token.
                settings = GetSettings( true );
                
            } catch ( Exception ex ) {
                //System.Console.WriteLine( $"Error while loading configuration changes.{Environment.NewLine}{ex}" );
            } finally {
                // The token will change each time it reloads, so we need to register again.
                settings?.ChangeToken?.RegisterChangeCallback( OnConfigurationReload, null );
            }
        }

        internal Logger[] Loggers {
            get {
                lock ( _loggers ) {
                    return _loggers.Values.ToArray();
                }
            }
        }

        protected virtual Logger CreateLogger( string categoryName ) {
            return new Logger( this, categoryName );
        }

        private Func<string, LogLevel, bool> GetFilter( string name, LoggerSettings settings ) {
            if ( _filter != null ) {
                return _filter;
            }

            if ( settings != null ) {
                if ( settings.TryGetSwitch( name, out LogLevel level ) ) {
                    return level.GetFilter();
                }
                return settings.MinLevel.GetFilter();
            }

            return LogLevelUtil.False;
        }

        public virtual void Dispose() {
            Interlocked.Exchange( ref _optionsReloadToken, null )?.Dispose();
        }

    }
}