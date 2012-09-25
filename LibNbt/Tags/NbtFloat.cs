using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtFloat : NbtTag, INbtTagValue<float> {
        public float Value { get; set; }

        public NbtFloat() : this( "" ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtFloat(string tagName, float value)" )]
        public NbtFloat( float value ) : this( "", value ) {}


        public NbtFloat( string tagName, float value = 0f ) {
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
            Value = readStream.ReadSingle();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Float );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Float; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Float" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}