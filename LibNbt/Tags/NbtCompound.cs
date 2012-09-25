using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt.Tags {
    public class NbtCompound : NbtTag, ICollection<NbtTag> {
        [ContractAnnotation("=> void")]
        static void ThrowNoTagName() {
            throw new ArgumentException( "All tags in a compound tag must be named." );
        }


        internal override NbtTagType TagType {
            get { return NbtTagType.Compound; }
        }


        public Dictionary<string, NbtTag> Tags { get; private set; }


        public NbtCompound()
            : this( null ) {}


        public NbtCompound( string tagName )
            : this( tagName, new NbtTag[0] ) {}


        public NbtCompound( string tagName, IEnumerable<NbtTag> tags ) {
            Name = tagName;
            Tags = new Dictionary<string, NbtTag>();
            foreach( NbtTag tag in tags ) {
                if( tag.Name == null ) ThrowNoTagName();
                Tags.Add( tag.Name, tag );
            }
        }


        public NbtTag this[ string tagName ] {
            get { return Get<NbtTag>( tagName ); }
            set { Set( tagName, value ); }
        }


        public NbtTag Get( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            return Get<NbtTag>( tagName );
        }


        public T Get<T>( [NotNull] string tagName ) where T : NbtTag {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( Tags.TryGetValue( tagName, out result ) ) {
                return (T)result;
            }
            return null;
        }


        public void Set( [NotNull] string tagName, [NotNull] NbtTag tag ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            if( tag == null ) throw new ArgumentNullException( "tag" );
            Tags[tagName] = tag;
        }


        #region Reading Tag

        internal override void ReadTag( NbtReader readStream ) {
            ReadTag( readStream, true );
        }


        internal override void ReadTag( NbtReader readStream, bool readName ) {
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
                        nextByte.ReadTag( readStream );
                        Add( nextByte );
                        break;

                    case NbtTagType.Short:
                        var nextShort = new NbtShort();
                        nextShort.ReadTag( readStream );
                        Add( nextShort );
                        break;

                    case NbtTagType.Int:
                        var nextInt = new NbtInt();
                        nextInt.ReadTag( readStream );
                        Add( nextInt );
                        break;

                    case NbtTagType.Long:
                        var nextLong = new NbtLong();
                        nextLong.ReadTag( readStream );
                        Add( nextLong );
                        break;

                    case NbtTagType.Float:
                        var nextFloat = new NbtFloat();
                        nextFloat.ReadTag( readStream );
                        Add( nextFloat );
                        break;

                    case NbtTagType.Double:
                        var nextDouble = new NbtDouble();
                        nextDouble.ReadTag( readStream );
                        Add( nextDouble );
                        break;

                    case NbtTagType.ByteArray:
                        var nextByteArray = new NbtByteArray();
                        nextByteArray.ReadTag( readStream );
                        Add( nextByteArray );
                        break;

                    case NbtTagType.String:
                        var nextString = new NbtString();
                        nextString.ReadTag( readStream );
                        Add( nextString );
                        break;

                    case NbtTagType.List:
                        var nextList = new NbtList();
                        nextList.ReadTag( readStream );
                        Add( nextList );
                        break;

                    case NbtTagType.Compound:
                        var nextCompound = new NbtCompound();
                        nextCompound.ReadTag( readStream );
                        Add( nextCompound );
                        break;

                    default:
                        throw new Exception( String.Format("Unsupported tag type found in NBT_Compound: {0}", nextTag) );
                }
            }
        }

        #endregion


        #region Writing Tag

        internal override void WriteTag( NbtWriter writeStream ) {
            WriteTag( writeStream, true );
        }


        internal override void WriteTag( NbtWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Compound );
            if( writeName ) {
                writeStream.Write( Name );
            }

            WriteData( writeStream );
        }


        internal override void WriteData( NbtWriter writeStream ) {
            foreach( NbtTag tag in Tags.Values ) {
                tag.WriteTag( writeStream );
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
                NbtTag nextTag = Get( nextToken.Name );
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