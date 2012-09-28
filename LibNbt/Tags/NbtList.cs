using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt {
    public sealed class NbtList : NbtTag, IList<NbtTag>, IList {
        internal override NbtTagType TagType {
            get { return NbtTagType.List; }
        }

        [NotNull] readonly List<NbtTag> tags;


        public NbtTagType ListType {
            get { return listType; }
            set {
                foreach( var tag in tags ) {
                    if( tag.TagType != value ) {
                        throw new Exception( "All list items must be of specified tag type." );
                    }
                }
                listType = value;
            }
        }

        NbtTagType listType;


        public NbtList()
            : this( null, null, NbtTagType.Unknown ) { }


        public NbtList( [CanBeNull] string tagName )
            : this( tagName, null, NbtTagType.Unknown ) { }


        public NbtList( [CanBeNull] IEnumerable<NbtTag> tags )
            : this( null, tags, NbtTagType.Unknown ) { }


        public NbtList( NbtTagType givenListType )
            : this( null, null, givenListType ) { }


        public NbtList( [CanBeNull] string tagName, [CanBeNull] IEnumerable<NbtTag> tags )
            : this( tagName, tags, NbtTagType.Unknown ) { }


        public NbtList( [CanBeNull] IEnumerable<NbtTag> tags, NbtTagType givenListType )
            : this( null, tags, givenListType ) { }


        public NbtList( [CanBeNull] string tagName, NbtTagType givenListType )
            : this( tagName, null, givenListType ) { }


        public NbtList( [CanBeNull] string tagName, [CanBeNull] IEnumerable<NbtTag> tags, NbtTagType givenListType ) {
            Name = tagName;
            this.tags = new List<NbtTag>();
            listType = givenListType;

            if( tags != null ) {
                this.tags.AddRange( tags );
                if( this.tags.Count > 0 ) {
                    if( ListType == NbtTagType.Unknown ) {
                        listType = this.tags[0].TagType;
                    }
                    foreach( NbtTag tag in this.tags ) {
                        if( tag.TagType != listType ) {
                            throw new ArgumentException( String.Format( "All tags must be of type {0}", listType ),
                                                         "tags" );
                        }
                    }
                }
            }
        }


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


        [NotNull]
        public T Get<T>( int tagIndex ) where T : NbtTag {
            return (T)tags[tagIndex];
        }


        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null ) throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


        [NotNull]
        public NbtTag[] ToArray() {
            return tags.ToArray();
        }


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


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            // read list type, and make sure it's defined
            ListType = readStream.ReadTagType();
            if( !Enum.IsDefined( typeof( NbtTagType ), ListType ) || ListType == NbtTagType.Unknown ) {
                throw new NbtParsingException( "Unrecognized TAG_List tag type: " + ListType );
            }

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtParsingException( "Negative count given in TAG_List" );
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
                if( Name == null ) throw new NullReferenceException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            writeStream.Write( ListType );
            writeStream.Write( tags.Count );
            foreach( NbtTag tag in tags ) {
                tag.WriteData( writeStream );
            }
        }

        #endregion


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


        #region Implementation of IEnumerable<NBtTag> and IEnumerable

        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.GetEnumerator();
        }

        #endregion


        #region Implementation of IList<NbtTag> and ICollection<NbtTag>

        public int IndexOf( NbtTag item ) {
            return tags.IndexOf( item );
        }


        public void Insert( int index, NbtTag item ) {
            if( listType == NbtTagType.Unknown ) {
                listType = item.TagType;
            } else if( item.TagType != listType ) {
                throw new ArgumentException( "Items must be of type " + listType );
            }
            tags.Insert( index, item );
        }


        public void RemoveAt( int index ) {
            tags.RemoveAt( index );
        }


        public void Add( NbtTag item ) {
            if( listType == NbtTagType.Unknown ) {
                listType = item.TagType;
            } else if( item.TagType != listType ) {
                throw new ArgumentException( "Items must be of type " + listType );
            }
            tags.Add( item );
        }


        public void Clear() {
            tags.Clear();
        }


        public bool Contains( NbtTag item ) {
            return tags.Contains( item );
        }


        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.CopyTo( array, arrayIndex );
        }


        public bool Remove( NbtTag item ) {
            return tags.Remove( item );
        }


        public int Count {
            get { return tags.Count; }
        }


        public bool IsReadOnly {
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

        #endregion
    }
}