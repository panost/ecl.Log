using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ecl.Log.Types;
using Microsoft.Extensions.Logging;

namespace ecl.Log.Formatters {
    /// <summary>
    /// The default formatter
    /// </summary>
    public class LoggerFormatter {
        protected readonly FormatterSettings Settings;
        public readonly Logger Logger;
        public StringBuilder Builder;

        /// <summary>
        /// 
        /// </summary>
        public TypeHandler Handler { get; private set; }

        
        public LoggerFormatter( Logger logger, FormatterSettings settings ) {
            Logger = logger;
            Settings = settings;
        }
        protected internal virtual void Init( StringBuilder b, TypeHandler handler ) {
            Builder = b;
            Handler = handler;
        }
        private void AppendParameter( ParameterInfo prm, char format = 'S' ) {
            switch ( format ) {
            case 'S':
            case 'F':
                AppendType( prm.ParameterType, format );
                if ( !string.IsNullOrEmpty(prm.Name) ) {
                    Builder.Append( " " );
                    goto case 'N';
                }
                break;
            case 'N':
                Builder.Append( prm.Name );
                break;
            case 'f':
                format = 'F';
                goto default;
            default:
                AppendType( prm.ParameterType, format );
                break;
            }
        }
        public void AppendMethod( MethodBase method, char parameterFormat = 'S' ) {
            if ( method == null )
                return;
            var declType = method.DeclaringType;
            if ( declType != null ) {
                AppendType( declType );
                Builder.Append( "." );
            }
            Builder.Append( method.Name );
            string delim;
            if ( method.IsGenericMethod ) {
                delim = "<";
                foreach ( Type argType in method.GetGenericArguments() ) {
                    Builder.Append( delim );
                    AppendType( argType, 'G' );
                    delim = ",";
                }
                Builder.Append( ">" );
            }
            if ( parameterFormat == ' ' )
                return;
            delim = "(";
            foreach ( var prm in method.GetParameters() ) {
                Builder.Append( delim );
                AppendParameter( prm, parameterFormat );
                delim = ", ";
            }
            Builder.Append( ")" );
        }
        protected internal virtual void AppendException( Exception error ) {
            AppendExceptionHead( error );
            if ( Settings.InnerException ) {
                while ( true ) {
                    Exception next = error.InnerException;
                    if ( next == null ) {
                        break;
                    }
                    error = next;
                    AppendExceptionHead( error );
                }
            }
            if ( Settings.MaxStackFrames > 0 ) {
                AppendStackTrace( error );
            }
        }

        private void AppendStackTrace( Exception error ) {
            var tr = new StackTrace( error, false );
            var count = tr.FrameCount;
            if ( count > Settings.MaxStackFrames ) {
                count = Settings.MaxStackFrames;
            }
            BeginSection( "Stack frame" );
            for ( int i = 0; i < count; i++ ) {
                var frame = tr.GetFrame( i );
                AppendStackFrame( frame );
            }
            CloseSection();
        }

        protected virtual void AppendTypeName( Type tp ) {
            Builder.Append( tp.Name );
        }
        protected virtual void AppendNamespace( string nameSpace ) {
            Builder.Append( nameSpace );
        }
        public virtual void AppendType( Type tp, char format = 'F' ) {
            if ( tp != null ) {
                if ( format == 'F' ) {
                    var declType = tp.DeclaringType;
                    if ( declType != null ) {
                        AppendType( declType );
                    } else {
                        AppendNamespace( tp.Namespace );
                    }
                    Builder.Append( '.' );
                }
                AppendTypeName( tp );
            }
        }
        private int _indentation;
        protected virtual void AppendExceptionHead( Exception error, char typeFormat = 'S' ) {
            Builder.Append( ' ', _indentation ).Append( error.Message ).Append( " (" );
            AppendType( error.GetType(), typeFormat );
            Builder.AppendLine( ")" );
            if ( Settings.ExceptionProperties ) {
                AppendExceptionProperties( error );
            }
            if ( Settings.ExceptionData ) {
                AppendExceptionData( error.Data );
            }
        }
        private bool HandleFusionLog( Exception exception ) {

            string fusionLog;
            string fileName;
            FileLoadException fileLoad;
            BadImageFormatException badImageFormat;
            FileNotFoundException fileNotFound = exception as FileNotFoundException;
            if ( fileNotFound != null ) {
                fusionLog = fileNotFound.FusionLog;
                fileName = fileNotFound.FileName;
            } else if ( ( fileLoad = exception as FileLoadException ) != null ) {
                fusionLog = fileLoad.FusionLog;
                fileName = fileLoad.FileName;
            } else if ( ( badImageFormat = exception as BadImageFormatException ) != null ) {
                fusionLog = badImageFormat.FusionLog;
                fileName = badImageFormat.FileName;
            } else {
                return false;
            }
            if ( !string.IsNullOrEmpty(fusionLog) ) {
                BeginSection( fileName );
                AppendText( fusionLog );
                CloseSection();
            }

            return true;
        }
        protected void AppendStackFrame( StackFrame frame ) {
            BeginLine();
            MethodBase method = frame.GetMethod();
            AppendMethod( method );
            var offset = frame.GetILOffset();
            if ( offset > 0 ) {
                Builder.Append( " +" + offset );
            }
            CloseLine();
        }
        public virtual void BeginLine( string text = null ) {
                Builder.AppendLine( text );
        }

        public virtual void CloseLine( string text = null ) {
            Builder.AppendLine( text );
        }
        private void AppendText( string textLines ) {
            Builder.Append( textLines );
        }

        private void AppendExceptionProperties( Exception error ) {
            HandleFusionLog( error );
        }

        private void AppendExceptionData( IDictionary errorData ) {
            bool found = false;
            foreach ( object o in errorData ) {
                if ( o is System.Collections.DictionaryEntry entry ) {
                    string key = entry.Key?.ToString();
                    if ( !string.IsNullOrEmpty( key ) ) {
                        string value = entry.Value?.ToString();
                        if ( !string.IsNullOrEmpty( value ) ) {
                            if ( !found ) {
                                BeginProperties();
                                found = true;
                            }
                            AppendProperty( key, value );
                        }
                    }
                }
            }
            if ( found ) {
                CloseProperties();
            }
        }

        protected internal virtual void AppendHead(LogLevel level, EventId id, string message ) {
            Builder.Append( '[' )
                .Append( DateTime.UtcNow.ToString( "s", DateTimeFormatInfo.InvariantInfo ) )
                .Append( "] " )
                .Append( level.ToShortCut() );
            if ( !string.IsNullOrEmpty( id.Name ) ) {
                Builder.Append( '#' ).Append( id.Name ).Append( "' " );
            } else if ( id.Id != 0 ) {
                Builder.Append( '#' ).Append( id.Id ).Append( ' ' );
            }
            if ( !string.IsNullOrEmpty( message ) ) {
                Builder.Append( " :\"" ).Append( message ).Append( '"' );
            }
            Builder.AppendLine();
        }
        protected internal virtual void BeginSection(string caption=null) {
            Builder.AppendLine(caption);
            _indentation++;
        }
        protected internal virtual void CloseSection() {
            _indentation--;
        }
        protected internal virtual void BeginScalar() {
        }
        protected internal virtual void CloseScalar() {
        }
        protected internal virtual void BeginScopes() {
        }
        protected internal virtual void CloseScopes() {
        }
        protected internal virtual void BeginScope() {
        }
        protected internal virtual void CloseScope() {
        }
        protected internal virtual void BeginProperties() {
        }
        protected internal virtual void CloseProperties() {
        }

        protected internal virtual void AppendProperty( string name, object value ) {
            Builder.Append( "  " )
                .Append( name )
                .Append( ": " )
                .Append( value )
                .AppendLine();
        }

        private delegate LoggerFormatter FormatterCtor( Logger logger, FormatterSettings settings );

        private static readonly FormatterCtor
            _defaultFormatter = ( logger, settings ) => new LoggerFormatter( logger, settings );

        private static readonly Dictionary<string, FormatterCtor> _loggerTypes
            = new Dictionary<string, FormatterCtor>( StringComparer.OrdinalIgnoreCase );

        private static FormatterCtor Get( FormatterSettings settings ) {
            string fname = settings.Formatter;
            if ( !string.IsNullOrEmpty( fname ) ) {
                FormatterCtor ctor;
                lock ( _loggerTypes ) {
                    if ( !_loggerTypes.TryGetValue( fname, out ctor ) ) {
                        ctor = CreateCtor( fname );
                        if ( ctor != null ) {
                            _loggerTypes.Add( fname, ctor );
                        }
                    }
                }
                if ( ctor != null ) {
                    return ctor;
                }
            }
            return _defaultFormatter;
        }

        public static LoggerFormatter Get( Logger logger, FormatterSettings settings, out Func<LoggerFormatter> ctor ) {
            FormatterCtor fctor;
            if ( settings == null ) {
                settings = new FormatterSettings( null );
                fctor = _defaultFormatter;
            } else {
                fctor = Get( settings );
            }
            ctor = () => fctor( logger, settings );
            return fctor( logger, settings );
        }

        private static FormatterCtor CreateCtor( string fname ) {
            Type type = Type.GetType( fname, false, true );
            if ( type != null && type.IsSubclassOf( typeof( Logger ) ) ) {
                var ctor = type.GetConstructor( new[] {
                    typeof( Logger ),
                    typeof( FormatterSettings )
                } );
                if ( ctor != null ) {
                    ParameterExpression[] args = {
                        Expression.Parameter( typeof( Logger ) ),
                        Expression.Parameter( typeof( FormatterSettings ) )
                    };
                    return (FormatterCtor)
                        Expression.Lambda( typeof( FormatterCtor ),
                            Expression.New( ctor, args ), args ).Compile();
                }
            }
            return null;
        }

        internal void AppendScopes( Scope current ) {
            if ( current != null ) {
                BeginScopes();
                current.AppendTo( this );
                CloseScopes();
            }
        }

        public void Flush() {
            Logger.Provider.Write( Builder );
        }
    }
}
