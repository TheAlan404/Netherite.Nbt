using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt {
    public sealed class NbtCompound : NbtTag, ICollection<NbtTag>, ICollection {
        internal override NbtTagType TagType {
            get { return NbtTagType.Compound; }
        }

        private readonly Dictionary<string, NbtTag> tags;


        public NbtCompound()
            : this( null, new NbtTag[0] ) {}


        public NbtCompound( string tagName )
            : this( tagName, new NbtTag[0] ) {}


        public NbtCompound( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags ) {}


        public NbtCompound( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags ) {
            Name = tagName;
            this.tags = new Dictionary<string, NbtTag>();
            foreach( NbtTag tag in tags ) {
                if( tag.Name == null ) {
                    throw new ArgumentException( "All tags in a compound tag must be named." );
                }
                this.tags.Add( tag.Name, tag );
            }
        }


        public NbtTag this[ [NotNull] string tagName ] {
            [CanBeNull]
            get { return Get<NbtTag>( tagName ); }
            set {
                if( tagName == null ) throw new ArgumentNullException( "tagName" );
                if( value == null ) throw new ArgumentNullException( "value" );
                tags[tagName] = value;
            }
        }


        [CanBeNull]
        public T Get<T>( [NotNull] string tagName ) where T : NbtTag {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( tags.TryGetValue( tagName, out result ) ) {
                return (T)result;
            }
            return null;
        }


        [NotNull]
        public NbtTag[] ToArray() {
            NbtTag[] array = new NbtTag[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag;
            }
            return array;
        }


        [NotNull]
        public string[] ToNameArray() {
            string[] array = new string[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag.Name;
            }
            return array;
        }


        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null ) throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


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
                        throw new NbtParsingException( "Unsupported tag type found in NBT_Compound: " + nextTag );
                }
            }
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Compound );
            if( writeName ) {
                if( Name == null ) throw new NullReferenceException( "Name is null" );
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


        #region Query

        public override NbtTag Query( string query ) {
            return Query<NbtTag>( query );
        }


        public override T Query<T>( string query ) {
            var tagQuery = new TagQuery( query );

            return Query<T>( tagQuery );
        }


        internal override T Query<T>( TagQuery query, bool bypassCheck ) {
            if( !bypassCheck ) {
                TagQueryToken token = query.Next();

                if( token != null && !token.Name.Equals( Name ) ) {
                    return null;
                }
            }

            TagQueryToken nextToken = query.Peek();
            if( nextToken != null ) {
                NbtTag nextTag = Get<NbtTag>( nextToken.Name );
                if( nextTag == null ) {
                    return null;
                }

                return nextTag.Query<T>( query );
            }

            return (T)( (NbtTag)this );
        }

        #endregion


        #region Implementation of IEnumerable<NbtTag>

        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.Values.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.Values.GetEnumerator();
        }

        #endregion


        #region Implementation of ICollection<NbtTag>

        public void Add( NbtTag item ) {
            if( item.Name == null ) {
                throw new ArgumentException( "Only named tags are allowed in compound tags." );
            }
            tags.Add( item.Name, item );
        }


        public void Clear() {
            tags.Clear();
        }


        public bool Contains( NbtTag item ) {
            return tags.ContainsValue( item );
        }


        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.Values.CopyTo( array, arrayIndex );
        }


        public bool Remove( [NotNull] NbtTag item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            if( item.Name == null ) throw new ArgumentException( "Trying to remove an unnamed tag." );
            NbtTag maybeItem;
            if( tags.TryGetValue( item.Name, out maybeItem ) ) {
                if( maybeItem == item ) {
                    return tags.Remove( item.Name );
                }
            }
            return false;
        }


        public int Count {
            get { return tags.Count; }
        }


        public bool IsReadOnly {
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
    }
}