using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> A tag containing a set of other named tags. Order is not guaranteed. </summary>
    public sealed class NbtCompound : NbtTag, ICollection<NbtTag>, ICollection {
        /// <summary> Type of this tag (Compound). </summary>
        public override NbtTagType TagType {
            get { return NbtTagType.Compound; }
        }

        readonly Dictionary<string, NbtTag> tags = new Dictionary<string, NbtTag>();


        /// <summary> Creates an empty unnamed NbtByte tag. </summary>
        public NbtCompound(){}


        /// <summary> Creates an empty NbtByte tag with the given name. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        public NbtCompound( [CanBeNull] string tagName ) {
            Name = tagName;
        }


        /// <summary> Creates an unnamed NbtByte tag, containing the given tags. </summary>
        /// <param name="tags"> Collection of tags to assign to this tag's Value. May not be null </param>
        /// <exception cref="ArgumentNullException"> If tags is null, or one of the tags is null. </exception>
        /// <exception cref="ArgumentException"> If some of the given tags were not named, or two tags with the same name were given. </exception>
        public NbtCompound( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags ) {}



        /// <summary> Creates an NbtByte tag with the given name, containing the given tags. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be null. </param>
        /// <param name="tags"> Collection of tags to assign to this tag's Value. May not be null </param>
        /// <exception cref="ArgumentNullException"> If tags is null, or one of the tags is null. </exception>
        /// <exception cref="ArgumentException"> If some of the given tags were not named, or two tags with the same name were given. </exception>
        public NbtCompound( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags ) {
            if( tags == null ) throw new ArgumentNullException( "tags" );
            Name = tagName;
            foreach( NbtTag tag in tags ) {
                Add( tag );
            }
        }


        /// <summary> Gets or sets the tag with the specified name. May return null. </summary>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <param name="tagName"> The name of the tag to get or set. </param>
        /// <exception cref="ArgumentNullException"> If tagName is null, or if trying to assign null value. </exception>
        public NbtTag this[ [NotNull] string tagName ] {
            [CanBeNull]
            get { return Get<NbtTag>( tagName ); }
            set {
                if( tagName == null ) throw new ArgumentNullException( "tagName" );
                if( value == null ) throw new ArgumentNullException( "value" );
                tags[tagName] = value;
            }
        }


        /// <summary> Gets or sets the tag with the specified name. May return null. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <exception cref="ArgumentNullException"> If tagName is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If tagName is null. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        [CanBeNull]
        public T Get<T>( [NotNull] string tagName ) where T : NbtTag {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( tags.TryGetValue( tagName, out result ) ) {
                return (T)result;
            }
            return null;
        }


        /// <summary> Copies all tags in this NbtCompound to an array. </summary>
        /// <returns> Array of NbtTags. </returns>
        [NotNull]
        public NbtTag[] ToArray() {
            NbtTag[] array = new NbtTag[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag;
            }
            return array;
        }


        /// <summary> Copies names of all tags in this NbtCompound to an array. </summary>
        /// <returns> Array of strings (tag names). </returns>
        [NotNull]
        public string[] ToNameArray() {
            string[] array = new string[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag.Name;
            }
            return array;
        }


        /// <summary> Adds all tags from the specified collection to this NbtCompound. </summary>
        /// <param name="newTags"> The collection whose elements should be added to this NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> If newTags is null, or one of the tags in newTags is null. </exception>
        /// <exception cref="ArgumentException"> If one of the given tags was unnamed,
        /// or if a tag with the given name already exists in this NbtCompound. </exception>
        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null ) throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


        /// <summary> Determines whether this NbtCompound contains a tag with a specific name. </summary>
        /// <param name="tagName"> Tag name to search for. May not be null. </param>
        /// <returns> true if a tag with given name was found; otherwise, false. </returns>
        /// <exception cref="ArgumentNullException"> If tagName is null. </exception>
        public bool Contains( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            return tags.ContainsKey( tagName );
        }


        /// <summary> Removes the tag with the specified name from this NbtCompound. </summary>
        /// <param name="tagName"> The name of the tag to remove. </param>
        /// <returns> true if the tag is successfully found and removed; otherwise, false.
        /// This method returns false if name is not found in the NbtCompound. </returns>
        /// <exception cref="ArgumentNullException"> If tagName is null. </exception>
        public bool Remove( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            return tags.Remove( tagName );
        }


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            tags.Clear();
            bool foundEnd = false;
            while( !foundEnd ) {
                NbtTagType nextTag = readStream.ReadTagType();
                switch( nextTag ) {
                    case NbtTagType.End:
                        foundEnd = true;
                        break;

                    case NbtTagType.Byte:
                        var nextByte = new NbtByte();
                        nextByte.ReadTag( readStream, true );
                        Add( nextByte );
                        //Console.WriteLine( nextByte.ToString() );
                        break;

                    case NbtTagType.Short:
                        var nextShort = new NbtShort();
                        nextShort.ReadTag( readStream, true );
                        Add( nextShort );
                        //Console.WriteLine( nextShort.ToString() );
                        break;

                    case NbtTagType.Int:
                        var nextInt = new NbtInt();
                        nextInt.ReadTag( readStream, true );
                        Add( nextInt );
                        //Console.WriteLine( nextInt.ToString() );
                        break;

                    case NbtTagType.Long:
                        var nextLong = new NbtLong();
                        nextLong.ReadTag( readStream, true );
                        Add( nextLong );
                        //Console.WriteLine( nextLong.ToString() );
                        break;

                    case NbtTagType.Float:
                        var nextFloat = new NbtFloat();
                        nextFloat.ReadTag( readStream, true );
                        Add( nextFloat );
                        //Console.WriteLine( nextFloat.ToString() );
                        break;

                    case NbtTagType.Double:
                        var nextDouble = new NbtDouble();
                        nextDouble.ReadTag( readStream, true );
                        Add( nextDouble );
                        //Console.WriteLine( nextDouble.ToString() );
                        break;

                    case NbtTagType.ByteArray:
                        var nextByteArray = new NbtByteArray();
                        nextByteArray.ReadTag( readStream, true );
                        Add( nextByteArray );
                        //Console.WriteLine( nextByteArray.ToString() );
                        break;

                    case NbtTagType.String:
                        var nextString = new NbtString();
                        nextString.ReadTag( readStream, true );
                        Add( nextString );
                        //Console.WriteLine( nextString.ToString() );
                        break;

                    case NbtTagType.List:
                        var nextList = new NbtList();
                        nextList.ReadTag( readStream, true );
                        Add( nextList );
                        //Console.WriteLine( nextList.ToString() );
                        break;

                    case NbtTagType.Compound:
                        var nextCompound = new NbtCompound();
                        nextCompound.ReadTag( readStream, true );
                        Add( nextCompound );
                        //Console.WriteLine( nextCompound.ToString() );
                        break;

                    case NbtTagType.IntArray:
                        var nextIntArray = new NbtIntArray();
                        nextIntArray.ReadTag( readStream, true );
                        Add( nextIntArray );
                        //Console.WriteLine( nextIntArray.ToString() );
                        break;

                    default:
                        throw new NbtFormatException( "Unsupported tag type found in NBT_Compound: " + nextTag );
                }
            }
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Compound );
            if( writeName ) {
                if( Name == null ) throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }

            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            foreach( NbtTag tag in tags.Values ) {
                tag.WriteTag( writeStream, true );
            }
            writeStream.Write( NbtTagType.End );
        }

        #endregion


        #region Implementation of IEnumerable<NbtTag>

        /// <summary> Returns an enumerator that iterates through all tags in this NbtCompound. </summary>
        /// <returns> An IEnumerator&gt;NbtTag&lt; that can be used to iterate through the collection. </returns>
        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.Values.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.Values.GetEnumerator();
        }

        #endregion


        #region Implementation of ICollection<NbtTag>

        /// <summary> Adds a tag to this NbtCompound. </summary>
        /// <param name="newTag"> The object to add to this NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> If newTag is null. </exception>
        /// <exception cref="ArgumentException"> If the given tag is unnamed;
        /// or if a tag with the given name already exists in this NbtCompound. </exception>
        public void Add( [NotNull] NbtTag newTag ) {
            if( newTag == null ) throw new ArgumentNullException( "newTag" );
            if( newTag == this ) throw new ArgumentException( "Cannot add tag to self" );
            if( newTag.Name == null ) {
                throw new ArgumentException( "Only named tags are allowed in compound tags." );
            }
            tags.Add( newTag.Name, newTag );
        }


        /// <summary> Removes all tags from this NbtCompound. </summary>
        public void Clear() {
            tags.Clear();
        }


        /// <summary> Determines whether this NbtCompound contains a specific NbtTag.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag is found; otherwise, false. </returns>
        /// <param name="tag"> The object to locate in this NbtCompound. May not be null. </param>
        /// <exception cref="ArgumentNullException"> If tag is null. </exception>
        public bool Contains( [NotNull] NbtTag tag ) {
            if( tag == null ) throw new ArgumentNullException( "tag" );
            return tags.ContainsValue( tag );
        }


        /// <summary> Copies the tags of the NbtCompound to an array, starting at a particular array index. </summary>
        /// <param name="array"> The one-dimensional array that is the destination of the tag copied from NbtCompound.
        /// The array must have zero-based indexing. </param>
        /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> arrayIndex is less than 0. </exception>
        /// <exception cref="ArgumentException"> Given array is multidimensional; arrayIndex is equal to or greater than the length of array;
        /// the number of tags in this NbtCompound is greater than the available space from arrayIndex to the end of the destination array;
        /// or type NbtTag cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.Values.CopyTo( array, arrayIndex );
        }


        /// <summary> Removes the first occurrence of a specific NbtTag from the NbtCompound.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag was successfully removed from the NbtCompound; otherwise, false.
        /// This method also returns false if tag is not found. </returns>
        /// <param name="tag"> The tag to remove from the NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> If tag is null. </exception>
        /// <exception cref="ArgumentException"> If the given tag is unnamed </exception>
        public bool Remove( [NotNull] NbtTag tag ) {
            if( tag == null ) throw new ArgumentNullException( "tag" );
            if( tag.Name == null ) throw new ArgumentException( "Trying to remove an unnamed tag." );
            NbtTag maybeItem;
            if( tags.TryGetValue( tag.Name, out maybeItem ) ) {
                if( maybeItem == tag ) {
                    return tags.Remove( tag.Name );
                }
            }
            return false;
        }


        /// <summary> Gets the number of tags contained in the NbtCompound. </summary>
        /// <returns> The number of tags contained in the NbtCompound. </returns>
        public int Count {
            get { return tags.Count; }
        }


        bool ICollection<NbtTag>.IsReadOnly {
            get { return false; }
        }

        #endregion


        #region Implementation of ICollection

        void ICollection.CopyTo( Array array, int index ) {
            CopyTo( (NbtTag[])array, index );
        }


        object ICollection.SyncRoot {
            get { return ( tags as ICollection ).SyncRoot; }
        }


        bool ICollection.IsSynchronized {
            get { return false; }
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtCompound object and its contents.
        /// Format: TAG_Compound("Name"): { ...contents... } </summary>
        /// <returns> A String that represents the current NbtCompound object and its contents. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Compound" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries\n", tags.Count );

            sb.Append( "{\n" );
            foreach( NbtTag tag in tags.Values ) {
                sb.AppendFormat( "\t{0}\n", tag.ToString().Replace( "\n", "\n\t" ) );
            }
            sb.Append( "}" );
            return sb.ToString();
        }
    }
}