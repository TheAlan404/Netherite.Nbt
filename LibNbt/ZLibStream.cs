using System.IO;
using System.IO.Compression;

namespace LibNbt {
    sealed class ZLibStream : DeflateStream {
        int adler32A = 1,
            adler32B;
        const int ChecksumModulus = 65521;

        public int Checksum {
            // Adler32 checksum
            get { return ( ( adler32B * 65536 ) + adler32A ); }
        }


        public ZLibStream( Stream stream, CompressionMode mode ) :
            base( stream, mode ) { }

        public ZLibStream( Stream stream, CompressionMode mode, bool leaveOpen ) :
            base( stream, mode, leaveOpen ) { }


        void UpdateChecksum( byte[] data, int offset, int length ) {
            for( int counter = 0; counter < length; ++counter ) {
                adler32A = ( adler32A + ( data[offset + counter] ) ) % ChecksumModulus;
                adler32B = ( adler32B + adler32A ) % ChecksumModulus;
            }
        }


        public override void Write( byte[] array, int offset, int count ) {
            UpdateChecksum( array, offset, count );
            base.Write( array, offset, count );
        }
    }
}