using System;
using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtInt : NbtTag, INbtTagValue<int> {
        public int Value { get; set; }

        public NbtInt() : this( "" ) {}


        [Obsolete( "This constructor will be removed in favor of using NbtInt(string tagName, int value)" )]
        public NbtInt( int value ) : this( "", value ) {}


        public NbtInt( string name, int value = 0 ) {
            Name = name;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream ) {
            ReadTag( readStream, true );
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


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Int );
            if( writeName ) {
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Int; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Int" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}