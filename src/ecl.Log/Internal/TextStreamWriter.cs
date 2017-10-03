using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ecl.Log {
    class TextStreamWriter {
        private Stream _baseStream;

        public Stream BaseStream => _baseStream;

        private bool _async;
        /// <summary>
        /// 
        /// </summary>
        public bool Async => _async;

        private byte[] _buffer;
        private char[] _chars;
        private int _charsLength;
        private Encoder _encoder;
        private Task _pendingWrite;

        public TextStreamWriter( Stream stream, Encoding encoding, int bufferSize,
            bool async ) {
            if ( encoding == null ) {
                encoding = Encoding.UTF8;
            }
            _chars=new char[ bufferSize ];
            bool writePreamble = stream.CanSeek && stream.Position == 0;
            _buffer = new byte[ encoding.GetMaxByteCount( bufferSize ) ];

            if ( writePreamble ) {
                byte[] pr = encoding.GetPreamble();
                stream.Write( pr, 0, pr.Length );
            }
            _encoder = encoding.GetEncoder();
            _baseStream = stream;
            _async = async;
        }
        private void EnsureWritten() {
            Interlocked.Exchange( ref _pendingWrite, null )?.Wait();
        }
        private int FlushBuffer( bool allowAsync = true) {
            bool async = allowAsync && _async;
            int length = _buffer.Length;
            int aval = length;
            
            EnsureWritten();

            int charsUsed;
            int bytesUsed;
            bool completed;

            _encoder.Convert( _chars, 0, _charsLength, _buffer, 0, aval, true, out charsUsed, out bytesUsed, out completed );
            if ( async ) {
                _pendingWrite = _baseStream.WriteAsync( _buffer, 0, bytesUsed );
            } else {
                _baseStream.Write( _buffer, 0, bytesUsed );
            }
            _charsLength -= charsUsed;
            if ( _charsLength > 0 ) {
                Array.Copy( _chars, charsUsed, _chars, 0, _charsLength );
            }
            return charsUsed;
        }


        public void Flush() {
            while ( _charsLength > 0 ) {
                FlushBuffer( false );
            }
            _baseStream.Flush();
        }
        public void Write( StringBuilder b ) {
            int bLength = b.Length;
            int index = 0;
            while ( bLength > 0 ) {
                int count = _chars.Length - _charsLength;
                if ( count < bLength ) {
                    count += FlushBuffer();
                }
                if ( count > bLength ) {
                    count = bLength;
                }
                b.CopyTo( index, _chars, _charsLength, count );
                _charsLength += count;
                index += count;
                bLength -= count;
            }
        }
        public void Write( string b ) {
            int bLength = b.Length;
            int index = 0;
            while ( bLength > 0 ) {
                int count = _chars.Length - _charsLength;
                if ( count < bLength ) {
                    count += FlushBuffer();
                }
                if ( count > bLength ) {
                    count = bLength;
                }
                b.CopyTo( index, _chars, _charsLength, count );
                _charsLength += count;
                index += count;
                bLength -= count;
            }
        }
        public virtual void Dispose() {
            if ( _baseStream != null ) {
                Flush();
                EnsureWritten();
                Interlocked.Exchange( ref _baseStream, null )?.Dispose();
            }
        }
    }
}
