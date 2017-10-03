using Microsoft.Extensions.Configuration;
using System;
namespace ecl.Log.File {
    public class FileLoggerSettings : LoggerSettings {
        public FileLoggerSettings( IConfiguration configuration )
            : base( configuration ) {
        }

        public FileLoggerSettings() {
        }

        private string _fileName;
        /// <summary>
        /// 
        /// </summary>
        public string FileName {
            get {
                if ( _fileName == null && Configuration != null ) {
                    _fileName = Configuration[ "FileName" ] ?? "";
                }
                return _fileName;
            }
            set {
                _fileName = value;
            }
        }

        private const FileRollOver Unknown = (FileRollOver)10;

        private FileRollOver _rollOver = Unknown;
        /// <summary>
        /// 
        /// </summary>
        public FileRollOver RollOver {
            get {
                if ( _rollOver == Unknown ) {
                    _rollOver = Configuration.GetEnum( "RollOver", FileRollOver.Daily );
                }
                return _rollOver;
            }
            set {
                _rollOver = value;
            }
        }

        private int _maxFileSize=-1;
        /// <summary>
        /// 
        /// </summary>
        public int MaxFileSize {
            get {
                if ( _maxFileSize < 0 ) {
                    _maxFileSize = Configuration.GetInt32( "MaxFileSize" );
                }
                return _maxFileSize;
            }
            set {
                _maxFileSize = value;
            }
        }

        public FileLoggerSettings Reload() {
            if ( Configuration != null ) {
                Reloaded();
                return new FileLoggerSettings( Configuration );
            }
            return this;
        }
    }
}
