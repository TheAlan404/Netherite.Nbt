using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtShort : NbtTag, INbtTagValue<short> {
        public short Value { get; set; }

        public NbtShort() : this( "" ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtShort(string tagName, short value)" )]
        public NbtShort( short value ) : this( "", value ) {}


        public NbtShort( string tagName, short value = 0 ) {
            Name = tagName;
            Value = value;
        }


        internal static short ReadShort( Stream readStream ) {
            var buffer = new byte[2];
            int numRead = readStream.Read( buffer, 0, buffer.Length );
            if( numRead != 2 ) throw new EndOfStreamException();
            if( BitConverter.IsLittleEndian ) Array.Reverse( buffer );
            return BitConverter.ToInt16( buffer, 0 );
        }


        internal override void ReadTag( NbtReader readStream ) {
            Name = readStream.ReadString();
            Value = readStream.ReadInt16();
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt16();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            writeStream.Write( NbtTagType.Short );
            writeStream.Write( Name );
            writeStream.Write( Value );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Short );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Short; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Short" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}