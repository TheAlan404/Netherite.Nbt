using System;
using System.Collections.Generic;
using System.IO;


namespace fNbt {
    /// <summary> Represents a reader that provides fast, noncached, forward-only access to NBT data. </summary>
    public class NbtReader {
        NbtParseState state = NbtParseState.AtStreamBeginning;
        readonly NbtBinaryReader reader;
        readonly Stack<NbtReaderState> states = new Stack<NbtReaderState>();
        readonly long streamStartOffset;
        bool atValue;


        /// <summary> Initializes a new instance of the NbtReader class. </summary>
        /// <param name="stream"> Stream to read from. </param>
        /// <param name="bigEndian"> Whether NBT data is in Big-Endian encoding. </param>
        public NbtReader( Stream stream, bool bigEndian = true ) {
            streamStartOffset = stream.Position;
            reader = new NbtBinaryReader( stream, bigEndian );
            ReadToFollowing();
        }


        /// <summary> Gets the name of the root tag of this NBT stream. </summary>
        public string RootName { get; private set; }

        /// <summary> Gets the name of the parent tag. May be null (for root tags and descendants of list elements). </summary>
        public string ParentName { get; private set; }

        /// <summary> Gets the name of the current tag. May be null (for list elements and end tags). </summary>
        public string TagName { get; private set; }

        
        /// <summary> Gets the type of the parent tag. Returns TagType.Unknown if there is no parent tag. </summary>
        public NbtTagType ParentTagType { get; private set; }

        /// <summary> Gets the type of the current tag. </summary>
        public NbtTagType TagType { get; private set; }

        /// <summary> Whether tag that we are currently on is a list element. </summary>
        public bool IsListElement {
            get {
                return ( state == NbtParseState.InList );
            }
        }

        /// <summary> Whether current tag has a value to read. </summary>
        public bool HasValue {
            get {
                switch( TagType ) {
                case NbtTagType.Compound:
                case NbtTagType.End:
                case NbtTagType.List:
                    return false;
                default:
                    return true;
                case NbtTagType.Unknown:
                    ThrowNotRead();
                    return false;
                }
            }
        }
        
        /// <summary> Whether current tag has a name. </summary>
        public bool HasName {
            get {
                return ( TagName != null );
            }
        }


        /// <summary> Whether the current tag is TAG_Compound. </summary>
        public bool IsCompound {
            get {
                return ( TagType == NbtTagType.Compound );
            }
        }

        /// <summary> Whether the current tag is TAG_List. </summary>
        public bool IsList {
            get {
                return (TagType == NbtTagType.List);
            }
        }

        /// <summary> Whether the current tag is TAG_End. </summary>
        public bool IsEnd {
            get {
                return (TagType == NbtTagType.End);
            }
        }


        /// <summary> Stream from which data is being read. </summary>
        public Stream BaseStream {
            get {
                return reader.BaseStream;
            }
        }

        /// <summary> Gets the number of bytes from the beginning of the stream to the beginning of this tag. </summary>
        public int TagStartOffset { get; private set; }


        /// <summary> Gets the depth of the current tag in the hierarchy. RootTag is 0, its descendant tags are 1, etc. </summary>
        public int Depth { get; private set; }


        /// <summary> If the current tag is TAG_List, returns type of the list elements. </summary>
        public NbtTagType ListType { get; private set; }

        /// <summary> If the current tag is TAG_List, TAG_Byte_Array, or TAG_Int_Array, returns the number of elements. </summary>
        public int TagLength { get; private set; }
        
        /// <summary> If the current tag is TAG_List, returns index of the current elements. </summary>
        public int ListIndex { get; private set; }


        /// <summary> Reads the next tag from the stream. </summary>
        /// <returns> true if the next tag was read successfully; false if there are no more tags to read. </returns>
        public bool ReadToFollowing() {
            switch( state ) {
                case NbtParseState.AtStreamBeginning:
                    // read first tag, make sure it's a compound
                    if( reader.ReadTagType() != NbtTagType.Compound ) {
                        state = NbtParseState.Error;
                        throw new NbtFormatException( "Given NBT stream does not start with a TAG_Compound" );
                    }
                    TagType = NbtTagType.Compound;
                    // Read root name. Advance to the first inside tag.
                    ReadTagHeader( true );
                    RootName = TagName;
                    return true;

                case NbtParseState.AtCompoundBeginning:
                    GoDown();
                    state = NbtParseState.InCompound;
                    goto case NbtParseState.InCompound;

                case NbtParseState.InCompound:
                    if( atValue )
                        SkipValue();
                    // Read next tag, check if we've hit the end
                    TagStartOffset = (int)( reader.BaseStream.Position - streamStartOffset );
                    NbtTagType nextTagType = reader.ReadTagType();
                    if( nextTagType == NbtTagType.End ) {
                        state = NbtParseState.AtCompoundEnd;
                        if( SkipEndTags ) {
                            goto case NbtParseState.AtCompoundEnd;
                        } else {
                            return true;
                        }
                    } else {
                        TagType = nextTagType;
                        ReadTagHeader( true );
                        return true;
                    }

                case NbtParseState.AtListBeginning:
                    GoDown();
                    state = NbtParseState.InList;
                    goto case NbtParseState.InList;

                case NbtParseState.InList:
                    if( atValue )
                        SkipValue();
                    ListIndex++;
                    if( ListIndex > TagLength ) {
                        GoUp();
                        return ReadToFollowing();
                    } else {
                        TagStartOffset = (int)( reader.BaseStream.Position - streamStartOffset );
                        ReadTagHeader( false );
                    }
                    return true;

                case NbtParseState.AtCompoundEnd:
                    if( Depth == 0 ) {
                        state = NbtParseState.AtStreamEnd;
                        return false;
                    } else {
                        GoUp();
                        return true;
                    }

                case NbtParseState.AtStreamEnd:
                    // nothing left to read!
                    return false;

                case NbtParseState.Error:
                    // previous call produced a parsing error
                    throw new InvalidOperationException( "NbtReader is in an erroneous state!" );
            }
            return true;
        }


        void ReadTagHeader( bool readName ) {
            if( readName ) {
                TagName = reader.ReadString();
            }
            switch( TagType ) {
                case NbtTagType.Byte:
                case NbtTagType.Double:
                case NbtTagType.Float:
                case NbtTagType.Long:
                case NbtTagType.Short:
                case NbtTagType.Int:
                case NbtTagType.String:
                    atValue = true;
                    break;

                case NbtTagType.IntArray:
                case NbtTagType.ByteArray:
                    TagLength = reader.ReadInt32();
                    atValue = true;
                    break;

                case NbtTagType.List:
                    ListType = reader.ReadTagType();
                    TagLength = reader.ReadInt32();
                    ListIndex = 0;
                    state = NbtParseState.AtListBeginning;
                    atValue = false;
                    break;

                case NbtTagType.Compound:
                    GoDown();
                    state = NbtParseState.AtCompoundBeginning;
                    atValue = false;
                    break;

                default:
                    throw new NbtFormatException( "Trying to read tag of unknown type." );
            }
        }


        // Goes one step down the NBT file's hierarchy, preserving current state
        void GoDown() {
            NbtReaderState newState = new NbtReaderState {
                ListIndex = ListIndex,
                TagLength = TagLength,
                ParentName = ParentName,
                ParentTagType = ParentTagType
            };
            states.Push( newState );

            ParentName = TagName;
            ParentTagType = TagType;
            ListIndex = 0;
            TagLength = 0;

            Depth++;

            if( TagType == NbtTagType.Compound ) {
                state = NbtParseState.InCompound;
            } else if( TagType == NbtTagType.List ) {
                state = NbtParseState.InList;
            } else {
                throw new NbtFormatException( "Trying to traverse the children of a tag that is neither a List nor a Compound!" );
            }
        }


        // Goes one step up the NBT file's hierarchy, restoring previous state
        void GoUp() {
            NbtReaderState oldState = states.Pop();

            ParentName = oldState.ParentName;
            ParentTagType = oldState.ParentTagType;
            ListIndex = oldState.ListIndex;
            TagLength = oldState.TagLength;

            Depth--;

            if( ParentTagType == NbtTagType.List ) {
                state = NbtParseState.InList;
            } else if( ParentTagType == NbtTagType.Compound ) {
                state = NbtParseState.InCompound;
            } else {
                state = NbtParseState.Error;
                throw new NbtFormatException( "Tag parent is neither a List nor a Compound!" );
            }
        }


        void SkipValue() {
            switch( TagType ) {
                case NbtTagType.Byte:
                    reader.ReadByte();
                    break;

                case NbtTagType.Short:
                    reader.ReadInt16();
                    break;

                case NbtTagType.Float:
                case NbtTagType.Int:
                    reader.ReadInt32();
                    break;

                case NbtTagType.Double:
                case NbtTagType.Long:
                    reader.ReadInt64();
                    break;

                case NbtTagType.ByteArray:
                    reader.Skip( TagLength );
                    break;

                case NbtTagType.IntArray:
                    reader.Skip( sizeof( int ) * TagLength );
                    break;

                default:
                    throw new InvalidOperationException( "Trying to skip value of a non-value tag." );
            }
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

        
        static void ThrowNotRead() {
            throw new InvalidOperationException( "No data has been read yet!" );
        }


        /// <summary> Parsing option: Whether NbtReader should skip End tags in ReadToFollowing() automatically while parsing. </summary>
        public bool SkipEndTags { get; set; }
    }
}