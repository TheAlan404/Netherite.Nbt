using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> A tag containing a single signed 32-bit integer. </summary>
    public sealed class NbtInt : NbtTag, INbtTagValue<int> {
        public override NbtTagType TagType {
            get { return NbtTagType.Int; }
        }

        public int Value { get; set; }


        public NbtInt()
            : this( null ) {}


        public NbtInt( int value )
            : this( null, value ) {}


        public NbtInt( [CanBeNull] string tagName )
            : this( tagName, 0 ) {}


        public NbtInt( [CanBeNull] string tagName, int value ) {
            Name = tagName;
            Value = value;
        }


        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt32();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Int );
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
            sb.Append( "TAG_Int" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}