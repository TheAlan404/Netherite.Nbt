using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtDouble : NbtTag, INbtTagValue<double> {
        internal override NbtTagType TagType {
            get { return NbtTagType.Double; }
        }

        public double Value { get; set; }


        public NbtDouble()
            : this( null ) {}


        public NbtDouble( double value )
            : this( null, value ) {}


        public NbtDouble( [CanBeNull] string tagName )
            : this( tagName, 0 ) {}


        public NbtDouble( [CanBeNull] string tagName, double value ) {
            Name = tagName;
            Value = value;
        }


        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadDouble();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Double );
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
            sb.Append( "TAG_Double" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}