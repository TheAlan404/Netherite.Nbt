using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> A tag containing an array of bytes. </summary>
    public sealed class NbtByteArray : NbtTag, INbtTagValue<byte[]> {
        /// <summary> Type of this tag (ByteArray). </summary>
        public override NbtTagType TagType {
            get { return NbtTagType.ByteArray; }
        }


        /// <summary> Value/payload of this tag (an array of bytes). May not be null. </summary>
        /// <exception cref="ArgumentNullException"> If given value is null. </exception>
        [NotNull]
        public byte[] Value {
            get { return bytes; }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                bytes = value;
            }
        }

        [NotNull]
        byte[] bytes;


        /// <summary> Creates an unnamed NbtByte tag, containing an empty array of bytes. </summary>
        public NbtByteArray()
            : this( null, new byte[0] ) { }


        /// <summary> Creates an unnamed NbtByte tag, containing the given array of bytes. </summary>
        /// <param name="value"> Byte array to assign to this tag's Value. May not be null. </param>
        /// <exception cref="ArgumentNullException"> If given value is null. </exception>
        public NbtByteArray( [NotNull] byte[] value )
            : this( null, value ) { }


        /// <summary> Creates an NbtByte tag with the given name, containing an empty array of bytes. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        public NbtByteArray( [CanBeNull] string tagName )
            : this( tagName, new byte[0] ) {}


        /// <summary> Creates an NbtByte tag with the given name, containing the given array of bytes. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="value"> Byte array to assign to this tag's Value. May not be null. </param>
        /// <exception cref="ArgumentNullException"> If given value is null. </exception>
        public NbtByteArray( [CanBeNull] string tagName, [NotNull] byte[] value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = (byte[])value.Clone();
        }


        /// <summary> Gets or sets a byte at the given index. </summary>
        /// <param name="index"> The zero-based index of the element to get or set. </param>
        /// <returns> The byte at the specified index. </returns>
        /// <exception cref="IndexOutOfRangeException"> If given index was outside the array bounds. </exception>
        public byte this[ int index ] {
            get { return Value[index]; }
            set { Value[index] = value; }
        }


        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative length given in TAG_Byte_Array" );
            }

            Value = readStream.ReadBytes( length );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.ByteArray );
            if( writeName ) {
                if( Name == null ) throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value.Length );
            writeStream.Write( Value, 0, Value.Length );
        }


        /// <summary> Returns a String that represents the current NbtByteArray object.
        /// Format: TAG_Byte_Array("Name"): [N bytes] </summary>
        /// <returns> A String that represents the current NbtByteArray object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Byte_Array" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} bytes]", Value.Length );
            return sb.ToString();
        }
    }
}