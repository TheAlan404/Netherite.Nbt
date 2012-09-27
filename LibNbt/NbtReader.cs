using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Tags;

namespace LibNbt {
    class NbtReader : BinaryReader {
        readonly byte[] floatBuffer = new byte[sizeof( float )],
                        doubleBuffer = new byte[sizeof( double )];

        public NbtReader( [NotNull] Stream input )
            : base( input ) {}


        public NbtTagType ReadTagType() {
            return (NbtTagType)base.ReadByte();
        }


        public override short ReadInt16() {
            return IPAddress.NetworkToHostOrder( base.ReadInt16() );
        }


        public override int ReadInt32() {
            return IPAddress.NetworkToHostOrder( base.ReadInt32() );
        }


        public override long ReadInt64() {
            return IPAddress.NetworkToHostOrder( base.ReadInt64() );
        }


        public override float ReadSingle() {
            if( BitConverter.IsLittleEndian ) {
                BaseStream.Read( floatBuffer, 0, sizeof( float ) );
                Array.Reverse( floatBuffer );
                return BitConverter.ToSingle( floatBuffer, 0 );
            } else {
                return base.ReadSingle();
            }
        }


        public override double ReadDouble() {
            if( BitConverter.IsLittleEndian ) {
                BaseStream.Read( doubleBuffer, 0, sizeof( double ) );
                Array.Reverse( doubleBuffer );
                return BitConverter.ToDouble( doubleBuffer, 0 );
            } else {
                return base.ReadDouble();
            }
        }


        public override string ReadString() {
            short length = ReadInt16();
            if( length < 0 ) {
                throw new NbtParsingException( "Negative string length given!" );
            }
            byte[] stringData = ReadBytes( length );
            return Encoding.UTF8.GetString( stringData );
        }
    }
}