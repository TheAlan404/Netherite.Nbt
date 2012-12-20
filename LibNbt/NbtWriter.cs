using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    sealed class NbtWriter : BinaryWriter {
        public NbtWriter( [NotNull] Stream input )
            : base( input ) {}


        public void Write( NbtTagType value ) {
            Write( (byte)value );
        }


        public override void Write( short value ) {
            base.Write( IPAddress.HostToNetworkOrder( value ) );
        }


        public override void Write( int value ) {
            base.Write( IPAddress.HostToNetworkOrder( value ) );
        }


        public override void Write( long value ) {
            base.Write( IPAddress.HostToNetworkOrder( value ) );
        }


        public override void Write( float value ) {
            if( BitConverter.IsLittleEndian ) {
                byte[] floatBytes = BitConverter.GetBytes( value );
                Array.Reverse( floatBytes );
                Write( floatBytes );
            } else {
                base.Write( value );
            }
        }


        public override void Write( double value ) {
            if( BitConverter.IsLittleEndian ) {
                byte[] doubleBytes = BitConverter.GetBytes( value );
                Array.Reverse( doubleBytes );
                Write( doubleBytes );
            } else {
                base.Write( value );
            }
        }


        public override void Write( string value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            var bytes = Encoding.UTF8.GetBytes( value );
            Write( (short)bytes.Length );
            Write( bytes );
        }
    }
}