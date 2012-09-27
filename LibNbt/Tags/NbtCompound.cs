using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt {
    public class NbtCompound : NbtTag, ICollection<NbtTag> {
        internal override NbtTagType TagType {
            get { return NbtTagType.Compound; }
        }

        private Dictionary<string, NbtTag> Tags { get; set; }


        public NbtCompound()
            : this( null, new NbtTag[0] ) {}


        public NbtCompound( string tagName )
            : this( tagName, new NbtTag[0] ) {}


        public NbtCompound( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags ) {}


        public NbtCompound( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags ) {
            Name = tagName;
            Tags = new Dictionary<string, NbtTag>();
            foreach( NbtTag tag in tags ) {
                if( tag.Name == null ) {
                    throw new ArgumentException( "All tags in a compound tag must be named." );
                }
                Tags.Add( tag.Name, tag );
            }
        }


        public NbtTag this[ [NotNull] string tagName ] {
            [CanBeNull]
            get { return Get<NbtTag>( tagName ); }
            set {
                if( tagName == null ) throw new ArgumentNullException( "tagName" );
                if( value == null ) throw new ArgumentNullException( "value" );
                Tags[tagName] = value;
            }
        }


        [CanBeNull]
        public T Get<T>( [NotNull] string tagName ) where T : NbtTag {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( Tags.TryGetValue( tagName, out result ) ) {
                return (T)result;
            }
            return null;
        }


        #region Reading / Writing

        internal void ReadTag( NbtReader readStream, bool readName ) {
            // First read the name of this tag
            if( readName ) {
                Name = readStream.ReadString();
            }

            Tags.Clear();
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
                        break;

                    case NbtTagType.Short:
                        var nextShort = new NbtShort();
                        nextShort.ReadTag( readStream, true );
                        Add( nextShort );
                        break;

                    case NbtTagType.Int:
                        var nextInt = new NbtInt();
                        nextInt.ReadTag( readStream, true );
                        Add( nextInt );
                        break;

                    case NbtTagType.Long:
                        var nextLong = new NbtLong();
                        nextLong.ReadTag( readStream, true );
                        Add( nextLong );
                        break;

                    case NbtTagType.Float:
                        var nextFloat = new NbtFloat();
                        nextFloat.ReadTag( readStream, true );
                        Add( nextFloat );
                        break;

                    case NbtTagType.Double:
                        var nextDouble = new NbtDouble();
                        nextDouble.ReadTag( readStream, true );
                        Add( nextDouble );
                        break;

                    case NbtTagType.ByteArray:
                        var nextByteArray = new NbtByteArray();
                        nextByteArray.ReadTag( readStream, true );
                        Add( nextByteArray );
                        break;

                    case NbtTagType.String:
                        var nextString = new NbtString();
                        nextString.ReadTag( readStream, true );
                        Add( nextString );
                        break;

                    case NbtTagType.List:
                        var nextList = new NbtList();
                        nextList.ReadTag( readStream, true );
                        Add( nextList );
                        break;

                    case NbtTagType.Compound:
                        var nextCompound = new NbtCompound();
                        nextCompound.ReadTag( readStream, true );
                        Add( nextCompound );
                        break;

                    case NbtTagType.IntArray:
                        var nextIntArray = new NbtIntArray();
                        nextIntArray.ReadTag( readStream, true );
                        Add( nextIntArray );
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
            foreach( NbtTag tag in Tags.Values ) {
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


        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append( "TAG_Compound" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries\n", Tags.Count );

            sb.Append( "{\n" );
            foreach( NbtTag tag in Tags.Values ) {
                sb.AppendFormat( "\t{0}\n", tag.ToString().Replace( "\n", "\n\t" ) );
            }
            sb.Append( "}" );
            return sb.ToString();
        }


        #region Implementation of IEnumerable<NbtTag>

        public IEnumerator<NbtTag> GetEnumerator() {
            return Tags.Values.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return Tags.Values.GetEnumerator();
        }

        #endregion


        #region Implementation of ICollection<NbtTag>

        public void Add( NbtTag item ) {
            if( item.Name == null ) {
                throw new ArgumentException( "Only named tags are allowed in compound tags." );
            }
            Tags.Add( item.Name, item );
        }


        public void Clear() {
            Tags.Clear();
        }


        public bool Contains( NbtTag item ) {
            return Tags.ContainsValue( item );
        }


        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            Tags.Values.CopyTo( array, arrayIndex );
        }


        public bool Remove( [NotNull] NbtTag item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            if( item.Name == null ) throw new ArgumentException( "Trying to remove an unnamed tag." );
            NbtTag maybeItem;
            if( Tags.TryGetValue( item.Name, out maybeItem ) ) {
                if( maybeItem == item ) {
                    return Tags.Remove( item.Name );
                }
            }
            return false;
        }


        public int Count {
            get { return Tags.Count; }
        }


        public bool IsReadOnly {
            get { return false; }
        }

        #endregion


        public NbtTag[] ToArray() {
            NbtTag[] array = new NbtTag[Tags.Count];
            int i = 0;
            foreach( NbtTag tag in Tags.Values ) {
                array[i++] = tag;
            }
            return array;
        }
    }
}