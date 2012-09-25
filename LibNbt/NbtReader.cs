using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Tags;

namespace LibNbt {
    class NbtReader : BinaryReader {
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
                byte[] bytes = base.ReadBytes( sizeof( float ) );
                Array.Reverse( bytes );
                return BitConverter.ToSingle( bytes, 0 );
            } else {
                return base.ReadSingle();
            }
        }


        public override double ReadDouble() {
            if( BitConverter.IsLittleEndian ) {
                byte[] bytes = base.ReadBytes( sizeof( double ) );
                Array.Reverse( bytes );
                return BitConverter.ToDouble( bytes, 0 );
            } else {
                return base.ReadDouble();
            }
        }


        public override string ReadString() {
            short length = ReadInt16();
            byte[] stringData = ReadBytes( length );
            return Encoding.UTF8.GetString( stringData );
        }
    }
}