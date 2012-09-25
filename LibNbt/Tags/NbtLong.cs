using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtLong : NbtTag, INbtTagValue<long> {
        public long Value { get; set; }

        public NbtLong() : this( "" ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtLong(string tagName, long value)" )]
        public NbtLong( long value ) : this( "", value ) {}


        public NbtLong( string tagName, long value = 0 ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream ) {
            ReadTag( readStream, true );
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt64();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Long );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Long; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Long" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}