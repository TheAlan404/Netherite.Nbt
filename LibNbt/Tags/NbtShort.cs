using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> A tag containing a single signed 16-bit integer. </summary>
    public sealed class NbtShort : NbtTag {
        /// <summary> Type of this tag (Short). </summary>
        public override NbtTagType TagType {
            get { return NbtTagType.Short; }
        }

        /// <summary> Value/payload of this tag (a single signed 16-bit integer). </summary>
        public short Value { get; set; }


        /// <summary> Creates an unnamed NbtShort tag with the default value of 0. </summary>
        public NbtShort() {}


        /// <summary> Creates an unnamed NbtShort tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtShort( short value )
            : this( null, value ) {}


        /// <summary> Creates an NbtShort tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        public NbtShort( [CanBeNull] string tagName )
            : this( tagName, 0 ) {}


        /// <summary> Creates an NbtShort tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtShort( [CanBeNull] string tagName, short value ) {
            Name = tagName;
            Value = value;
        }


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            if( readName ) {
                Name = readStream.ReadString();
            }
            Value = readStream.ReadInt16();
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Short );
            if( writeName ) {
                if( Name == null ) throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( Value );
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtShort object.
        /// Format: TAG_Short("Name"): Value </summary>
        /// <returns> A String that represents the current NbtShort object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Short" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }
}