using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> A tag containing a single byte. </summary>
    public sealed class NbtByte : NbtTag, INbtTagValue<byte> {
        /// <summary> Type of this tag (Byte). </summary>
        public override NbtTagType TagType {
            get { return NbtTagType.Byte; }
        }

        /// <summary> Value/payload of this tag (a single byte). </summary>
        public byte Value { get; set; }


        /// <summary> Creates an unnamed NbtByte tag with the default value of 0. </summary>
        public NbtByte()
            : this( null, 0 ) {}


        /// <summary> Creates an unnamed NbtByte tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtByte( byte value )
            : this( null, value ) {}


        /// <summary> Creates an NbtByte tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        public NbtByte( [CanBeNull] string tagName )
            : this( tagName, 0 ) {}



        /// <summary> Creates an NbtByte tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtByte( [CanBeNull] string tagName, byte value ) {
            Name = tagName;
            Value = value;
        }


        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadByte();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Byte );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }


        /// <summary> Returns a String that represents the current NbtByte object.
        /// Format: TAG_Byte("Name"): Value </summary>
        /// <returns> A String that represents the current NbtByte object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0}", Value );
            return sb.ToString();
        }
    }
}