using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    public sealed class NbtIntArray : NbtTag, INbtTagValue<int[]> {
        [NotNull]
        public int[] Value { get; set; }


        public int this[ int index ] {
            get { return Value[index]; }
            set { Value[index] = value; }
        }


        public NbtIntArray()
            : this( null, new int[0] ) {}


        public NbtIntArray( [NotNull] int[] value )
            : this( null, value ) {}


        public NbtIntArray( [CanBeNull] string tagName )
            : this( tagName, new int[0] ) {}


        public NbtIntArray( [CanBeNull] string tagName, [NotNull] int[] value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = (int[])value.Clone();
        }


        internal void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtParsingException( "Negative length given in TAG_Int_Array" );
            }

            Value = new int[length];
            for( int i = 0; i < length; i++ ) {
                Value[i] = readStream.ReadInt32();
            }
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.IntArray );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value.Length );
            for( int i = 0; i < Value.Length; i++ ) {
                writeStream.Write( Value[i] );
            }
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.IntArray; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Int_Array" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} ints]", Value.Length );
            return sb.ToString();
        }
    }
}