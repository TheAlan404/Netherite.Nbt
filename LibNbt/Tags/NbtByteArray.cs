using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtByteArray : NbtTag, INbtTagValue<byte[]> {
        public byte[] Value { get; set; }


        public byte this[ int index ] {
            get { return Value[index]; }
            set { Value[index] = value; }
        }


        public NbtByteArray() : this( "" ) {}

        public NbtByteArray( string tagName ) : this( tagName, new byte[] { } ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtByteArray(string tagName, byte[] value)" )]
        public NbtByteArray( byte[] value ) : this( "", value ) {}


        public NbtByteArray( string tagName, byte[] value ) {
            Name = tagName;
            Value = new byte[value.Length];
            Buffer.BlockCopy( value, 0, Value, 0, value.Length );
        }


        internal override void ReadTag( NbtReader readStream ) {
            ReadTag( readStream, true );
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new Exception( "Negative length given in TAG_Byte_Array" );
            }

            Value = readStream.ReadBytes( length );
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.ByteArray );
            if( writeName ) {
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value.Length );
            writeStream.Write( Value, 0, Value.Length );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.ByteArray; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte_Array" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} bytes]", Value.Length );
            return sb.ToString();
        }
    }
}