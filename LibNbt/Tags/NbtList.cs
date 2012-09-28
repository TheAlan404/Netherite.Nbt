using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt {
    /// <summary> A tag containing a list of unnamed tags, all of the same kind. </summary>
    public sealed class NbtList : NbtTag, IList<NbtTag>, IList {
        /// <summary> Type of this tag (List). </summary>
        public override NbtTagType TagType {
            get { return NbtTagType.List; }
        }

        [NotNull]
        readonly List<NbtTag> tags;


        /// <summary> Gets or sets the tag type of this list. All tags in this NbtTag must be of the same type. </summary>
        /// <exception cref="ArgumentException"> If the given NbtTagType does not match the type of existing list items (for non-empty lists). </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        public NbtTagType ListType {
            get { return listType; }
            set {
                if( !Enum.IsDefined( typeof( NbtTagType ), value ) ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                foreach( var tag in tags ) {
                    if( tag.TagType != value ) {
                        throw new ArgumentException( "All list items must be of specified tag type." );
                    }
                }
                listType = value;
            }
        }

        NbtTagType listType;


        /// <summary> Creates an unnamed NbtList with empty contents and undefined ListType. </summary>
        public NbtList()
            : this( null, null, NbtTagType.Unknown ) { }


        /// <summary> Creates an NbtList with given name, empty contents, and undefined ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        public NbtList( [CanBeNull] string tagName )
            : this( tagName, null, NbtTagType.Unknown ) { }


        /// <summary> Creates an unnamed NbtList with the given contents, and inferred ListType. 
        /// If given tag array is empty, NbtTagType remains Unknown. </summary>
        /// <param name="tags"> Collection of tags to insert into the list. All tags are expected to be of the same type.
        /// ListType is inferred from the first tag. List may be empty, but may not be null. </param>
        /// <exception cref="ArgumentNullException"> If tags is null. </exception>
        /// <exception cref="ArgumentException"> If given tags are of mixed types. </exception>
        public NbtList( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags, NbtTagType.Unknown ) {
            if( tags == null ) throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an unnamed NbtList with empty contents and an explicitly specified ListType.
        /// If ListType is Unknown, it will be inferred from the type of the first added tag.
        /// Otherwise, all tags added to this list are expected to be of the given type. </summary>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown. </param>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        public NbtList( NbtTagType givenListType )
            : this( null, null, givenListType ) { }


        /// <summary> Creates an NbtList with the given name and contents, and inferred ListType. 
        /// If given tag array is empty, NbtTagType remains Unknown. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="tags"> Collection of tags to insert into the list. All tags are expected to be of the same type.
        /// ListType is inferred from the first tag. List may be empty, but may not be null. </param>
        /// <exception cref="ArgumentNullException"> If tags is null. </exception>
        /// <exception cref="ArgumentException"> If given tags are of mixed types. </exception>
        public NbtList( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags )
            : this( tagName, tags, NbtTagType.Unknown ) {
            if( tags == null ) throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an unnamed NbtList with the given contents, and an explicitly specified ListType. </summary>
        /// <param name="tags"> Collection of tags to insert into the list.
        /// All tags are expected to be of the same type (matching givenListType).
        /// List may be empty, but may not be null. </param>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown (to infer type from the first element of tags). </param>
        /// <exception cref="ArgumentNullException"> If tags is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        /// <exception cref="ArgumentException"> If not all given tags are of givenListType; or if given tags are of mixed types. </exception>
        public NbtList( [NotNull] IEnumerable<NbtTag> tags, NbtTagType givenListType )
            : this( null, tags, givenListType ) {
            if( tags == null ) throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an NbtList with the given name, empty contents, and an explicitly specified ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="givenListType"> Name to assign to this tag.
        /// If givenListType is Unknown, ListType will be infered from the first tag added to this NbtList. </param>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        public NbtList( [CanBeNull] string tagName, NbtTagType givenListType )
            : this( tagName, null, givenListType ) { }


        /// <summary> Creates an NbtList with the given name and contents, and an explicitly specified ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="tags"> Collection of tags to insert into the list.
        /// All tags are expected to be of the same type (matching givenListType). May be empty or null. </param>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown (to infer type from the first element of tags). </param>
        /// <exception cref="ArgumentException"> If not all given tags are of givenListType; or if given tags are of mixed types. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        public NbtList( [CanBeNull] string tagName, [CanBeNull] IEnumerable<NbtTag> tags, NbtTagType givenListType ) {
            Name = tagName;
            this.tags = new List<NbtTag>();
            listType = givenListType;

            if( !Enum.IsDefined( typeof( NbtTagType ), givenListType ) ) {
                throw new ArgumentOutOfRangeException( "givenListType" );
            }

            if( tags != null ) {
                this.tags.AddRange( tags );
                if( this.tags.Count > 0 ) {
                    if( ListType == NbtTagType.Unknown ) {
                        listType = this.tags[0].TagType;
                    }
                    foreach( NbtTag tag in this.tags ) {
                        if( tag.TagType != listType ) {
                            throw new ArgumentException( "All tags must be of type " + listType, "tags" );
                        }
                    }
                }
            }
        }


        /// <summary> Gets or sets the tag at the specified index. </summary>
        /// <returns> The tag at the specified index. </returns>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <exception cref="ArgumentOutOfRangeException"> tagIndex is not a valid index in the NbtList. </exception>
        /// <exception cref="ArgumentNullException"> Given tag is null. </exception>
        /// <exception cref="ArgumentException"> Given tag's type does not match ListType. </exception>
        [NotNull]
        public NbtTag this[ int tagIndex ] {
            get { return tags[tagIndex]; }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                if( listType == NbtTagType.Unknown ) {
                    listType = value.TagType;
                } else if( value.TagType != listType ) {
                    throw new ArgumentException( "Items must be of type " + listType );
                }
                tags[tagIndex] = value;
            }
        }


        /// <summary> Gets or sets the tag with the specified name. </summary>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> The tag with the specified key. </returns>
        /// <exception cref="ArgumentNullException"> If tagName is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> tagIndex is not a valid index in the NbtList. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        [NotNull]
        public T Get<T>( int tagIndex ) where T : NbtTag {
            return (T)tags[tagIndex];
        }


        /// <summary> Adds all tags from the specified collection to the end of this NbtList. </summary>
        /// <param name="newTags"> The collection whose elements should be added to this NbtList. </param>
        /// <exception cref="ArgumentNullException"> If newTags is null. </exception>
        /// <exception cref="ArgumentException"> If one of the given tags was . </exception>
        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null ) throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


        /// <summary> Copies all tags in this NbtList to an array. </summary>
        /// <returns> Array of NbtTags. </returns>
        [NotNull]
        public NbtTag[] ToArray() {
            return tags.ToArray();
        }


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            // read list type, and make sure it's defined
            ListType = readStream.ReadTagType();
            if( !Enum.IsDefined( typeof( NbtTagType ), ListType ) || ListType == NbtTagType.Unknown ) {
                throw new NbtFormatException( "Unrecognized TAG_List tag type: " + ListType );
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative count given in TAG_List" );
            }

            tags.Clear();
            for( int i = 0; i < length; i++ ) {
                switch( ListType ) {
                    case NbtTagType.Byte:
                        var nextByte = new NbtByte();
                        nextByte.ReadTag( readStream, false );
                        tags.Add( nextByte );
                        break;
                    case NbtTagType.Short:
                        var nextShort = new NbtShort();
                        nextShort.ReadTag( readStream, false );
                        tags.Add( nextShort );
                        break;
                    case NbtTagType.Int:
                        var nextInt = new NbtInt();
                        nextInt.ReadTag( readStream, false );
                        tags.Add( nextInt );
                        break;
                    case NbtTagType.Long:
                        var nextLong = new NbtLong();
                        nextLong.ReadTag( readStream, false );
                        tags.Add( nextLong );
                        break;
                    case NbtTagType.Float:
                        var nextFloat = new NbtFloat();
                        nextFloat.ReadTag( readStream, false );
                        tags.Add( nextFloat );
                        break;
                    case NbtTagType.Double:
                        var nextDouble = new NbtDouble();
                        nextDouble.ReadTag( readStream, false );
                        tags.Add( nextDouble );
                        break;
                    case NbtTagType.ByteArray:
                        var nextByteArray = new NbtByteArray();
                        nextByteArray.ReadTag( readStream, false );
                        tags.Add( nextByteArray );
                        break;
                    case NbtTagType.String:
                        var nextString = new NbtString();
                        nextString.ReadTag( readStream, false );
                        tags.Add( nextString );
                        break;
                    case NbtTagType.List:
                        var nextList = new NbtList();
                        nextList.ReadTag( readStream, false );
                        tags.Add( nextList );
                        break;
                    case NbtTagType.Compound:
                        var nextCompound = new NbtCompound();
                        nextCompound.ReadTag( readStream, false );
                        tags.Add( nextCompound );
                        break;
                    case NbtTagType.IntArray:
                        var nextIntArray = new NbtIntArray();
                        nextIntArray.ReadTag( readStream, false );
                        tags.Add( nextIntArray );
                        break;
                }
            }
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.List );
            if( writeName ) {
                if( Name == null ) throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            if( ListType == NbtTagType.Unknown ) {
                throw new NbtFormatException( "NbtList had no elements and an Unknown ListType" );
            }
            writeStream.Write( ListType );
            writeStream.Write( tags.Count );
            foreach( NbtTag tag in tags ) {
                tag.WriteData( writeStream );
            }
        }

        #endregion


        #region Query

        public override NbtTag Query( string query ) {
            return Query<NbtTag>( query );
        }


        public override T Query<T>( string query ) {
            var tagQuery = new TagQuery( query );

            return Query<T>( tagQuery );
        }


        internal override T Query<T>( TagQuery query, bool bypassCheck ) {
            TagQueryToken token = query.Next();

            if( !bypassCheck ) {
                if( token != null && !token.Name.Equals( Name ) ) {
                    return null;
                }
            }

            var nextToken = query.Peek();
            if( nextToken != null ) {
                // Make sure this token is an integer because NbtLists don't have
                // named tag items
                int tagIndex;
                if( !int.TryParse( nextToken.Name, out tagIndex ) ) {
                    throw new NbtQueryException(
                        string.Format( "Attempt to query by name on a list tag that doesn't support names. ({0})",
                                       Name ) );
                }

                NbtTag indexedTag = Get<NbtTag>( tagIndex );

                if( query.TokensLeft() > 1 ) {
                    // Pop the index token so the current token is the next
                    // named token to continue the query
                    query.Next();

                    // Bypass the name check because the tag won't have one
                    return indexedTag.Query<T>( query, true );
                }

                return (T)indexedTag;
            }

            return (T)( (NbtTag)this );
        }

        #endregion


        #region Implementation of IEnumerable<NBtTag> and IEnumerable

        /// <summary> Returns an enumerator that iterates through all tags in this NbtList. </summary>
        /// <returns> An IEnumerator&gt;NbtTag&lt; that can be used to iterate through the list. </returns>
        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.GetEnumerator();
        }

        #endregion


        #region Implementation of IList<NbtTag> and ICollection<NbtTag>

        /// <summary> Determines the index of a specific tag in this NbtList </summary>
        /// <returns> The index of tag if found in the list; otherwise, -1. </returns>
        /// <param name="tag"> The tag to locate in this NbtList. </param>
        public int IndexOf( NbtTag tag ) {
            return tags.IndexOf( tag );
        }


        /// <summary> Inserts an item to this NbtList at the specified index. </summary>
        /// <param name="index"> The zero-based index at which newTag should be inserted. </param>
        /// <param name="newTag"> The tag to insert into this NbtList. </param>
        /// <exception cref="ArgumentOutOfRangeException"> index is not a valid index in this NbtList. </exception>
        public void Insert( int index, NbtTag newTag ) {
            if( listType == NbtTagType.Unknown ) {
                listType = newTag.TagType;
            } else if( newTag.TagType != listType ) {
                throw new ArgumentException( "Items must be of type " + listType );
            }
            tags.Insert( index, newTag );
        }


        /// <summary> Removes a tag at the specified index from this NbtList. </summary>
        /// <param name="index"> The zero-based index of the item to remove. </param>
        /// <exception cref="ArgumentOutOfRangeException"> Given index is not a valid index in the NbtList. </exception>
        public void RemoveAt( int index ) {
            tags.RemoveAt( index );
        }


        /// <summary> Adds an tag to this NbtList. </summary>
        /// <param name="newTag"> The tag to add to this NbtList. </param>
        /// <exception cref="ArgumentNullException"> If newTag is null. </exception>
        /// <exception cref="ArgumentException"> If newTag does not match ListType. </exception>
        public void Add( [NotNull] NbtTag newTag ) {
            if( newTag == null ) throw new ArgumentNullException( "newTag" );
            if( listType == NbtTagType.Unknown ) {
                listType = newTag.TagType;
            } else if( newTag.TagType != listType ) {
                throw new ArgumentException( "Items must be of type " + listType );
            }
            tags.Add( newTag );
        }


        /// <summary> Removes all tags from this NbtList. </summary>
        public void Clear() {
            tags.Clear();
        }


        /// <summary> Determines whether this NbtList contains a specific tag. </summary>
        /// <returns> true if given tag is found in this NbtList; otherwise, false. </returns>
        /// <param name="item"> The tag to locate in this NbtList. </param>
        public bool Contains( [NotNull] NbtTag item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            return tags.Contains( item );
        }


        /// <summary> Copies the tags of this NbtList to an array, starting at a particular array index. </summary>
        /// <param name="array"> The one-dimensional array that is the destination of the tag copied from NbtList.
        /// The array must have zero-based indexing. </param>
        /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> arrayIndex is less than 0. </exception>
        /// <exception cref="ArgumentException"> Given array is multidimensional; arrayIndex is equal to or greater than the length of array;
        /// the number of tags in this NbtList is greater than the available space from arrayIndex to the end of the destination array;
        /// or type NbtTag cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.CopyTo( array, arrayIndex );
        }


        /// <summary> Removes the first occurrence of a specific NbtTag from the NbtCompound.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag was successfully removed from this NbtList; otherwise, false.
        /// This method also returns false if tag is not found. </returns>
        /// <param name="tag"> The tag to remove from this NbtList. </param>
        /// <exception cref="ArgumentNullException"> If tag is null. </exception>
        public bool Remove( [NotNull] NbtTag tag ) {
            if( tag == null ) throw new ArgumentNullException( "tag" );
            return tags.Remove( tag );
        }


        /// <summary> Gets the number of tags contained in the NbtList. </summary>
        /// <returns> The number of tags contained in the NbtList. </returns>
        public int Count {
            get { return tags.Count; }
        }


        bool ICollection<NbtTag>.IsReadOnly {
            get { return false; }
        }

        #endregion


        #region Implementation of IList and ICollection

        void IList.Remove( object value ) {
            tags.Remove( (NbtTag)value );
        }


        object IList.this[int tagIndex] {
            get { return tags[tagIndex]; }
            set { this[tagIndex] = (NbtTag)value; }
        }


        int IList.Add( object value ) {
            Add( (NbtTag)value );
            return ( tags.Count - 1 );
        }


        bool IList.Contains( object value ) {
            return tags.Contains( (NbtTag)value );
        }


        int IList.IndexOf( object value ) {
            return tags.IndexOf( (NbtTag)value );
        }


        void IList.Insert( int index, object value ) {
            Insert( index, (NbtTag)value );
        }


        bool IList.IsFixedSize {
            get { return false; }
        }


        void ICollection.CopyTo( Array array, int index ) {
            CopyTo( (NbtTag[])array, index );
        }


        object ICollection.SyncRoot {
            get { return ( tags as ICollection ).SyncRoot; }
        }


        bool ICollection.IsSynchronized {
            get { return false; }
        }


        bool IList.IsReadOnly {
            get { return false; }
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtList object and its contents.
        /// Format: TAG_List("Name"): { ...contents... } </summary>
        /// <returns> A String that represents the current NbtList object and its contents. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_List" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries\n", tags.Count );

            sb.Append( "{\n" );
            foreach( NbtTag tag in tags ) {
                sb.AppendFormat( "\t{0}\n", tag.ToString().Replace( "\n", "\n\t" ) );
            }
            sb.Append( "}" );
            return sb.ToString();
        }
    }
}