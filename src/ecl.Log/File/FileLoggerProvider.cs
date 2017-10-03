using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ecl.Log.File {
    [ProviderAlias( "ecl.File" )]
    public partial class FileLoggerProvider: LoggerProvider {
        private FileLoggerSettings _settings;

        public FileLoggerProvider( IOptionsMonitor<FileLoggerSettings> options ) {
            _optionsReloadToken = options.OnChange( ReloadFileLoggerOptions );
            ReloadFileLoggerOptions( options.CurrentValue );
        }

        public FileLoggerProvider( FileLoggerSettings settings )
            : base( settings ) {
            ReloadFileLoggerOptions( settings );
        }

        private string _fileBaseName;
        private TextStreamWriter _file;
        private readonly object _fileSync = new object();
        private FileRollOver _rollOver;

        protected void ReloadFileLoggerOptions( FileLoggerSettings settings ) {
            lock ( _fileSync ) {
                bool resetStream = _rollOver != settings.RollOver;
                _rollOver = settings.RollOver;
                if ( settings.FileName != _fileBaseName ) {
                    _fileBaseName = settings.FileName;
                    resetStream = true;
                }

                if ( resetStream && _file != null ) {
                    Interlocked.Exchange( ref _file, null )?.Dispose();
                }
            }
            _settings = settings;
            ReloadLoggerOptions( settings );
        }
        /// <summary>
        /// When the metric is going to change
        /// </summary>
        private DateTime _metricDue;

        private void GetDateMetric(StringBuilder b, FileRollOver rollOver ) {
            var dt = DateTime.UtcNow;
            int year = dt.Year;
            int month = dt.Month;
            int day= dt.Day;
            int hour = dt.Hour;
            b.Append( year );
            b.Append( '-' );
            if ( month <= 9 )
                b.Append( '0' );
            b.Append( month );
            if ( rollOver >= FileRollOver.Daily ) {
                b.Append( '-' );
                if ( day <= 9 )
                    b.Append( '0' );
                b.Append( day );
                if ( rollOver >= FileRollOver.Hourly ) {
                    b.Append( 'T' );
                    if ( hour <= 9 )
                        b.Append( '0' );
                    b.Append( hour );
                }
            }
            switch ( rollOver ) {
            case FileRollOver.Monthly:
                _metricDue = new DateTime( year, month, 1, 0, 0, 0, DateTimeKind.Utc ).AddMonths( 1 );
                break;
            case FileRollOver.Daily:
                _metricDue = new DateTime( year, month, day, 0, 0, 0, DateTimeKind.Utc ).AddDays( 1 );
                break;
            case FileRollOver.Hourly:
                _metricDue = new DateTime( year, month, day, hour, 0, 0, DateTimeKind.Utc ).AddHours( 1 );
                break;
            default:
                // ?? Minutes from now
                int minutes = dt.Minute;
                b.Append( '-' );
                if ( minutes <= 9 )
                    b.Append( '0' );
                b.Append( minutes );
                // size checking every 4 minutes
                _metricDue = dt.AddMinutes( 4 );
                break;
            }
        }

        
        TextStreamWriter LockWriter() {
            Monitor.Enter( _fileSync );
            try {
                if ( _file != null ) {
                    if ( _metricDue > DateTime.UtcNow || !CloseCurrent() ) {
                        return _file;
                    }
                }
                return _file = CreateWriter();
            } catch {
                Monitor.Exit( _fileSync );
                throw;
            }
        }

        private bool CloseCurrent() {
            FileRollOver rollOver = _settings.RollOver;
            if ( rollOver == FileRollOver.Size ) {
                int maxSize = _settings.MaxFileSize;
                if ( maxSize <= 0 ) {
                    maxSize = 1024;
                }
                if ( _file.BaseStream.Length / ( 1024 * 1024 ) <= maxSize ) {
                    return false;
                }
            }
            Interlocked.Exchange( ref _file, null )?.Dispose();
            return true;
        }

        private TextStreamWriter CreateWriter() {
            string fileName = _fileBaseName;
            if ( string.IsNullOrEmpty( fileName ) ) {
                fileName = Path.GetFullPath( "log.log" );
            }
            DirectoryInfo dinfo = new DirectoryInfo( Path.GetDirectoryName( fileName ) );
            string name = Path.GetFileNameWithoutExtension( fileName );
            if ( !dinfo.Exists ) {
                dinfo.Create();
            }
            FileStream stream = null;

            using ( StringBuilderCache c = StringBuilderCache.Acquire() ) {
                c.Builder.Append( Path.Combine( dinfo.FullName, name ) ).Append( '-' );
                string ext = Path.GetExtension( fileName );
                FileRollOver rollOver = _settings.RollOver;
                if ( rollOver >= FileRollOver.Monthly
                     && rollOver <= FileRollOver.Hourly ) {
                    GetDateMetric( c, rollOver );
                } else if ( rollOver == FileRollOver.Size ) {
                    stream = GetCounterFile( c.Builder, dinfo, name, ext );
                } else {
                    _metricDue = DateTime.MaxValue;
                }
                c.Builder.Append( ext );
                fileName = c.ToString();
            }
            if ( stream == null ) {
                stream = new FileStream( fileName, FileMode.Append, FileAccess.Write, FileShare.Read );
            }
            //} catch ( IOException ) {
            //    fileName += Guid.NewGuid().ToString();
            //    stream = new FileStream( fileName, FileMode.Append, FileAccess.Write, FileShare.Read );
            //}
            var file = new TextStreamWriter( stream, Encoding.UTF8, 4096, true );
            using ( StringBuilderCache c = StringBuilderCache.Acquire() ) {
                c.Builder.Append( "## " )
                    .Append( stream.Length > 0 ? "Append" : "Created" )
                    .Append( '[' )
                    .Append( DateTime.UtcNow.ToString( "s", DateTimeFormatInfo.InvariantInfo ) )
                    .Append( ']' )
                    .Append( Environment.NewLine );
                file.Write( c );
            }
            return file;
        }

        private FileStream GetCounterFile( StringBuilder b, DirectoryInfo dinfo, string name, string ext ) {
            int startIndex = name.Length + 1;
            int extLength = ext.Length;
            int minLength = startIndex + extLength;
            int maxCounter = -1;
            FileInfo found = null;
            int maxSize = _settings.MaxFileSize;
            if ( maxSize <= 0 ) {
                maxSize = 1024;
            }
            foreach ( FileInfo file in dinfo.EnumerateFiles( name + "-*" + ext, SearchOption.TopDirectoryOnly ) ) {
                string patName = file.Name;
                if ( patName.Length > minLength ) {
                    string bareCounter = patName.Substring( startIndex, patName.Length - minLength );
                    if ( int.TryParse( bareCounter, NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out int counter ) ) {
                        if ( counter > maxCounter ) {
                            found = file;
                            maxCounter = counter;
                        }
                    }
                }
            }
            if ( found == null ) {
                maxCounter = 1;
            } else {
                if ( found.Length / ( 1024 * 1024 ) < maxSize ) {
                    try {
                        return new FileStream( found.FullName, FileMode.Append, FileAccess.Write,
                            FileShare.Read );
                    } catch ( IOException ) {

                    }
                }
                maxCounter++;
            }
            b.Append( '-' );
            int baseLen = b.Length;
            while ( true ) {
                b.Append( maxCounter );
                b.Append( ext );
                string fileName = b.ToString();
                if ( !System.IO.File.Exists( fileName ) ) {
                    try {
                        return new FileStream( fileName, FileMode.Append, FileAccess.Write,
                            FileShare.Read );
                    } catch ( IOException ) {
                        
                    }
                }
                maxCounter++;
                b.Length = baseLen;
            }
        }

        protected internal override void Write( StringBuilder b ) {
            TextStreamWriter file = LockWriter();
            try {
                file.Write( b );
            } finally {
                Monitor.Exit( _fileSync );
            }
        }

        public override void Dispose() {
            base.Dispose();
            Interlocked.Exchange( ref _file, null )?.Dispose();
        }

        protected internal override LoggerSettings GetSettings( bool reload = false ) {
            if ( reload ) {
                _settings = _settings.Reload();
                ReloadFileLoggerOptions( _settings );
            }
            return _settings;
        }
    }
}
