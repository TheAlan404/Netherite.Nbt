using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtByteArray : NbtTag, INbtTagValue<byte[]> {
        internal override NbtTagType TagType {
            get { return NbtTagType.ByteArray; }
        }

        public byte[] Value { get; set; }


        public NbtByteArray()
            : this( null, new byte[0] ) { }


        public NbtByteArray( [NotNull] byte[] value )
            : this( null, value ) { }


        public NbtByteArray( [CanBeNull] string tagName )
            : this( tagName, new byte[0] ) {}


        public NbtByteArray( [CanBeNull] string tagName, [NotNull] byte[] value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = (byte[])value.Clone();
        }


        public byte this[ int index ] {
            get { return Value[index]; }
            set { Value[index] = value; }
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


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.ByteArray );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value.Length );
            writeStream.Write( Value, 0, Value.Length );
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte_Array" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} bytes]", Value.Length );
            return sb.ToString();
        }
    }
}