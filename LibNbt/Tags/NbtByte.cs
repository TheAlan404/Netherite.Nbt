using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtByte : NbtTag, INbtTagValue<byte> {
        public byte Value { get; set; }

        public NbtByte() : this( null ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtByte(string tagName, byte value)" )]
        public NbtByte( byte value ) : this( null, value ) {}


        public NbtByte( string name, byte value = 0x00 ) {
            Name = name;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream ) {
            ReadTag( readStream, true );
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadByte();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Byte );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Byte; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}