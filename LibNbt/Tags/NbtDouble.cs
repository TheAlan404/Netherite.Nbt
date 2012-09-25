using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtDouble : NbtTag, INbtTagValue<double> {
        public double Value { get; set; }

        public NbtDouble() : this( "" ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtDouble(string tagName, double value)" )]
        public NbtDouble( double value ) : this( "", value ) {}


        public NbtDouble( string tagName, double value = 0.00f ) {
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
            Value = readStream.ReadDouble();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Double );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Double; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Double" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}