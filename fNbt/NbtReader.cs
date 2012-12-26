using System;
using System.IO;

namespace fNbt {
    /// <summary> Represents a reader that provides fast, noncached, forward-only access to NBT data. </summary>
    public class NbtReader {
        bool endOfStream;
        bool valueRead;
        readonly Stream stream;
        readonly long startPosition;
        readonly NbtBinaryReader reader;

        /// <summary> Initializes a new instance of the NbtReader class. </summary>
        /// <param name="stream"> Stream to read from. </param>
        /// <param name="bigEndian"> Whether NBT data is in Big-Endian encoding. </param>
        public NbtReader( Stream stream, bool bigEndian = true ) {
            this.stream = stream;
            startPosition = stream.Position;
            reader = new NbtBinaryReader( stream, bigEndian );
            Read();
        }


        /// <summary> Gets the name of the root tag of this NBT stream. </summary>
        public string RootName { get; private set; }

        /// <summary> Gets the name of the parent tag. May be null (for root tags and descendants of list elements). </summary>
        public string ParentName { get; private set; }

        /// <summary> Gets the name of the current tag. May be null (for list elements and end tags). </summary>
        public string TagName { get; private set; }


        /// <summary> Gets the type of the current tag. </summary>
        public NbtTagType TagType { get; private set; }

        /// <summary> Whether the current tag is TAG_Compound. </summary>
        public bool IsCompound { get; private set; }

        /// <summary> Whether the current tag is TAG_List. </summary>
        public bool IsList { get; private set; }

        /// <summary> Whether the current tag is TAG_End. </summary>
        public bool IsEnd { get; private set; }


        /// <summary> Gets the number of bytes from the beginning of the stream to the beginning of this tag. </summary>
        public int StreamOffset {
            get {
                return (int)( stream.Position - startPosition );
            }
        }

        /// <summary> Gets the depth of the current tag in the hierarchy. RootTag is 0, its descendant tags are 1, etc. </summary>
        public int Depth { get; private set; }


        /// <summary> If the current tag is TAG_List, returns type of the list elements. </summary>
        public NbtTagType ListType { get; private set; }

        /// <summary> If the current tag is TAG_List, TAG_Byte_Array, or TAG_Int_Array, returns the number of elements. </summary>
        public int TagLength { get; private set; }


        /// <summary> Reads the next tag from the stream. </summary>
        /// <returns> true if the next tag was read successfully; false if there are no more tags to read. </returns>
        public bool Read() {
            throw new NotImplementedException();
        }


        /// <summary> Skips the children of the current node. Does not begin to read the following tag. </summary>
        /// <returns> Number of child tags that were skipped. </returns>
        public int Skip() {
            throw new NotImplementedException();
        }


        /// <summary> Reads until a tag with the specified name is found. 
        /// Returns false if the end of stream is reached. </summary>
        /// <param name="tagName"> Name of the tag. </param>
        /// <returns> true if a matching tag is found; otherwise false. </returns>
        public bool ReadToFollowing( string tagName ) {
            throw new NotImplementedException();
        }


        /// <summary> Advances the NbtReader to the next descendant tag with the specified name.
        /// If a matching child tag is not found, the NbtReader is positioned on the end tag. </summary>
        /// <param name="tagName"> Name of the tag you wish to move to. </param>
        /// <returns> true if a matching descendant tag is found; otherwise false. </returns>
        public bool ReadToDescendant( string tagName ) {
            throw new NotImplementedException();
        }


        /// <summary> Advances the NbtReader to the next sibling tag with the specified name.
        /// If a matching sibling tag is not found, the NbtReader is positioned on the end tag of the parent tag. </summary>
        /// <param name="tagName"> The name of the sibling tag you wish to move to. </param>
        /// <returns> true if a matching sibling element is found; otherwise false. </returns>
        public bool ReadToNextSibling( string tagName ) {
            throw new NotImplementedException();
        }


        /// <summary> Reads the entirety of the current tag, including any descendants,
        /// and constructs an NbtTag object of the appropriate type. </summary>
        /// <returns> Constructed NbtTag object. </returns>
        public NbtTag ReadAsTag() {
            throw new NotImplementedException();
        }


        /// <summary> Reads the value as an object of the type specified. </summary>
        /// <typeparam name="T"> The type of the value to be returned.
        /// Tag value should be convertible to this type. </typeparam>
        /// <returns> Tag value converted to the requested type. </returns>
        public T ReadValueAs<T>() {
            throw new NotImplementedException();
        }
        
        /// <summary> Reads the value as an object of the correct type, boxed.
        /// Cannot be called for tags that do not have a single-object value (compound, list, and end tags). </summary>
        /// <returns> Tag value converted to the requested type. </returns>
        public object ReadValue() {
            throw new NotImplementedException();
        }

        /// <summary> Reads the value of this tag as an array.
        /// Current tag must be TAG_Byte_Array, TAG_Int_Array, or a TAG_List of primitive ListType. </summary>
        /// <typeparam name="T"> Element type of the array to be returned.
        /// Tag contents should be convertible to this type. </typeparam>
        /// <returns> Tag value converted to an array of the requested type. </returns>
        public T[] ReadValueAsArray<T>() {
            throw new NotImplementedException();
        }
    }
}