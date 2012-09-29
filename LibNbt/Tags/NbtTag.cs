using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> Base class for different kinds of named binary tags. </summary>
    public abstract class NbtTag {
        /// <summary> Type of this tag. </summary>
        public virtual NbtTagType TagType {
            get { return NbtTagType.Unknown; }
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
    }
}