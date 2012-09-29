using System;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> Base class for different kinds of named binary tags. </summary>
    public abstract class NbtTag {
        /// <summary> Type of this tag. </summary>
        public virtual NbtTagType TagType {
            get { return NbtTagType.Unknown; }
        }


        /// <summary> Gets or sets the tag with the specified name. May return null. </summary>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <param name="tagName"> The name of the tag to get or set. Must match tag's actual name. </param>
        /// <exception cref="InvalidOperationException"> If used on a tag that is not NbtCompound. </exception>
        /// <remarks> ONLY APPLICABLE TO NntCompound OBJECTS!
        /// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
        public virtual NbtTag this[string tagName] {
            get { throw new InvalidOperationException( "String indexers only work on NbtCompound tags." ); }
            set { throw new InvalidOperationException( "String indexers only work on NbtCompound tags." ); }
        }


        /// <summary> Gets or sets the tag at the specified index. </summary>
        /// <returns> The tag at the specified index. </returns>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <exception cref="ArgumentOutOfRangeException"> tagIndex is not a valid index in the NbtList. </exception>
        /// <exception cref="ArgumentNullException"> Given tag is null. </exception>
        /// <exception cref="ArgumentException"> Given tag's type does not match ListType. </exception>
        /// <exception cref="InvalidOperationException"> If used on a tag that is not NbtList. </exception>
        /// <remarks> ONLY APPLICABLE TO NbtList OBJECTS!
        /// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
        public virtual NbtTag this[ int tagIndex ] {
            get { throw new InvalidOperationException( "Integer indexers only work on NbtList tags." ); }
            set { throw new InvalidOperationException( "Integer indexers only work on NbtList tags." ); }
        }


        /// <summary> Name of this tag. Immutable, and set by the constructor. May be null. </summary>
        [CanBeNull]
        public string Name { get; protected set; }


        internal abstract void WriteTag( [NotNull] NbtWriter writeReader, bool writeName );


        // WriteData does not write the tag's ID byte or the name
        internal abstract void WriteData( [NotNull] NbtWriter writeReader );


        /// <summary> Returns a canonical (Notchy) name for the given NbtTagType,
        /// e.g. "TAG_Byte_Array" for NbtTagType.ByteArray </summary>
        /// <param name="type"> NbtTagType to name. </param>
        /// <returns> String representing the canonical name of a tag,
        /// or null of given TagType does not have a canonical name (e.g. Unknown). </returns>
        [CanBeNull]
        public static string GetCanonicalTagName( NbtTagType type ) {
            switch( type ) {
                case NbtTagType.Byte:
                    return "TAG_Byte";
                case NbtTagType.ByteArray:
                    return "TAG_Byte_Array";
                case NbtTagType.Compound:
                    return "TAG_Compound";
                case NbtTagType.Double:
                    return "TAG_Double";
                case NbtTagType.End:
                    return "TAG_End";
                case NbtTagType.Float:
                    return "TAG_Float";
                case NbtTagType.Int:
                    return "TAG_Int";
                case NbtTagType.IntArray:
                    return "TAG_Int_Array";
                case NbtTagType.List:
                    return "TAG_List";
                case NbtTagType.Long:
                    return "TAG_Long";
                case NbtTagType.Short:
                    return "TAG_Short";
                case NbtTagType.String:
                    return "TAG_String";
                default:
                    return null;
            }
        }

        
        /// <summary> Prints contents of this tag, and any child tags, to a string.
        /// Indents the string using multiples of the given indentation string. </summary>
        /// <param name="indentString"> String to be used for indentation. </param>
        /// <returns> A string representing contants of this tag, and all child tags (if any). </returns>
        /// <exception cref="ArgumentNullException"> identString is null. </exception>
        [NotNull]
        public string ToString( [NotNull] string indentString ) {
            if( indentString == null ) throw new ArgumentNullException( "indentString" );
            StringBuilder sb = new StringBuilder();
            PrettyPrint( sb, indentString, 0 );
            return sb.ToString();
        }


        internal abstract void PrettyPrint( StringBuilder sb, string indentString, int indentLevel );
    }
}