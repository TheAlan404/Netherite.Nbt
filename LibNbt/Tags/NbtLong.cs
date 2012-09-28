using System;
using System.Text;

namespace LibNbt {
    /// <summary> A tag containing a single signed 64-bit integer. </summary>
    public sealed class NbtLong : NbtTag, INbtTagValue<long> {
        public override NbtTagType TagType {
            get { return NbtTagType.Long; }
        }

        public long Value { get; set; }


        public NbtLong()
            : this( null ) { }


        public NbtLong( long value )
            : this( null, value ) {}


        public NbtLong( string tagName )
            : this( tagName, 0 ) {}


        public NbtLong( string tagName, long value ) {
            Name = tagName;
            Value = value;
        }


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt64();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Long );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }

        #endregion


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Long" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}