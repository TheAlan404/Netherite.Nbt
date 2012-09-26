using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtInt : NbtTag, INbtTagValue<int> {
        internal override NbtTagType TagType {
            get { return NbtTagType.Int; }
        }

        public int Value { get; set; }


        public NbtInt()
            : this( null ) {}


        public NbtInt( int value )
            : this( null, value ) {}


        public NbtInt( [CanBeNull] string tagName, int value = 0 ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                var name = new NbtString();
                name.ReadTag( readStream, false );

                Name = name.Value;
            }


            var buffer = new byte[4];
            int totalRead = 0;
            while( ( totalRead += readStream.Read( buffer, totalRead, 4 ) ) < 4 ) {}
            if( BitConverter.IsLittleEndian ) Array.Reverse( buffer );
            Value = BitConverter.ToInt32( buffer, 0 );
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