using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    public class NbtString : NbtTag, INbtTagValue<string> {
        internal override NbtTagType TagType {
            get { return NbtTagType.String; }
        }

        [CanBeNull]
        public string Value { get; set; }


        public NbtString()
            : this( null ) {}


        public NbtString( [CanBeNull] string tagName, [CanBeNull] string value = null ) {
            Name = tagName;
            Value = value;
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadString();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.String );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value ?? "" );
        }


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_String" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}