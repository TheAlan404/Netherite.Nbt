using System.IO;
using System.Text;

namespace LibNbt.Tags {
    public class NbtString : NbtTag, INbtTagValue<string> {
        public string Value { get; set; }

        public NbtString() : this( "" ) {}


        public NbtString( string tagName, string value = "" ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream ) {
            Name = readStream.ReadString();
            Value = readStream.ReadString();
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadString();
        }


        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( (byte)NbtTagType.String );
            if( writeName ) {
                var name = new NbtString( "", Name );
                name.WriteData( writeStream );
            }

            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            byte[] str = Encoding.UTF8.GetBytes( Value );

            var length = new NbtShort( "", (short)str.Length );
            length.WriteData( writeStream );

            writeStream.Write( str, 0, str.Length );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.String; }
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_String" );
            if( Name.Length > 0 ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}