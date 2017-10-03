using Microsoft.Extensions.Configuration;

namespace ecl.Log.Formatters {
    public class FormatterSettings {
        public readonly IConfiguration Configuration;
        /// <summary>
        /// 
        /// </summary>
        public FormatterSettings( IConfiguration configuration ) {
            Configuration = configuration;
        }

        private string _formatter;
        /// <summary>
        /// 
        /// </summary>
        public string Formatter {
            get {
                if ( _formatter == null && Configuration != null ) {
                    _formatter = Configuration[ "Formatter" ] ?? "";
                }
                return _formatter;
            }
            set {
                _formatter = value;
            }
        }
        private byte _exceptionData;
        public bool ExceptionData {
            get {
                if ( _exceptionData == 0 ) {
                    _exceptionData = Configuration.GetBoolean( "ExceptionData" )
                        ? (byte)1
                        : (byte)2;
                }
                return _exceptionData == 1;
            }
            set => _exceptionData = value ? (byte)1 : (byte)2;
        }
        private byte _exceptionProperties;
        public bool ExceptionProperties {
            get {
                if ( _exceptionProperties == 0 ) {
                    _exceptionProperties = Configuration.GetBoolean( "ExceptionProperties", true )
                        ? (byte)1
                        : (byte)2;
                }
                return _exceptionProperties == 1;
            }
            set => _exceptionProperties = value ? (byte)1 : (byte)2;
        }
        private byte _innerException;
        public bool InnerException {
            get {
                if ( _innerException == 0 ) {
                    _innerException = Configuration.GetBoolean( "InnerException" )
                        ? (byte)1
                        : (byte)2;
                }
                return _innerException == 1;
            }
            set => _innerException = value ? (byte)1 : (byte)2;
        }
        private int _maxStackFrames;
        /// <summary>
        /// The maximum stack frames to log
        /// <para>0 to disable stack frame</para>
        /// </summary>
        public int MaxStackFrames {
            get {
                if ( _maxStackFrames < 0 ) {
                    _maxStackFrames = Configuration.GetInt32( "MaxStackFrames" );
                }
                return _maxStackFrames;
            }
            set {
                _maxStackFrames = value;
            }
        }

    }
}
