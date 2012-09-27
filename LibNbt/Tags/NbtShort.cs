using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtShort : NbtTag, INbtTagValue<short> {
        internal override NbtTagType TagType {
            get { return NbtTagType.Short; }
        }

        public short Value { get; set; }


        public NbtShort()
            : this( null ) {}


        public NbtShort( short value )
            : this( null, value ) {}


        public NbtShort( [CanBeNull] string tagName )
            : this( tagName, 0 ) {}


        public NbtShort( [CanBeNull] string tagName, short value ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt16();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Short );
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
            sb.Append( "TAG_Short" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}