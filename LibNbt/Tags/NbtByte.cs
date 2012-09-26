using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtByte : NbtTag, INbtTagValue<byte> {
        internal override NbtTagType TagType {
            get { return NbtTagType.Byte; }
        }

        public byte Value { get; set; }


        public NbtByte()
            : this( null ) {}


        public NbtByte( byte value )
            : this( null, value ) {}


        public NbtByte( [CanBeNull] string tagName, byte value = 0 ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadByte();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Byte );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}