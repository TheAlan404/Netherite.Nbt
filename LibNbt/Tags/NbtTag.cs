using System;
using JetBrains.Annotations;
using LibNbt.Queries;

namespace LibNbt {
    public abstract class NbtTag {
        internal virtual NbtTagType TagType {
            get { return NbtTagType.Unknown; }
        }


        [CanBeNull]
        public string Name { get; protected set; }


        internal abstract void WriteTag( [NotNull] NbtWriter writeReader, bool writeName );


        // WriteData does not write the tag's ID byte or the name
        internal abstract void WriteData( [NotNull] NbtWriter writeReader );



        #region Query

        public virtual NbtTag Query( [NotNull] string query ) {
            return Query<NbtTag>( query );
        }


        public virtual T Query<T>( [NotNull] string query ) where T : NbtTag {
            if( query == null ) throw new ArgumentNullException( "query" );
            var tagQuery = new TagQuery( query );
            return Query<T>( tagQuery );
        }


        internal virtual T Query<T>( TagQuery query ) where T : NbtTag {
            return Query<T>( query, false );
        }


        /// <summary> Queries the tag to easily find a tag in a structure. </summary>
        /// <typeparam name="T"> Type of the tag to return. </typeparam>
        /// <param name="query"> Tokenized query </param>
        /// <param name="bypassCheck"> Bypass the name check when querying non-named queries.
        /// NbtList elements are an example. </param>
        /// <returns> The tag that was queried for. </returns>
        internal virtual T Query<T>( [NotNull] TagQuery query, bool bypassCheck ) where T : NbtTag {
            if( query == null ) throw new ArgumentNullException( "query" );
            if( bypassCheck ) {
                return (T)this;
            }

            TagQueryToken token = query.Next();

            if( token.Name.Equals( Name ) ) {
                if( query.Peek() != null ) {
                    throw new NbtQueryException( string.Format( "Attempt through non list type tag: {0}", Name ) );
                }

                return (T)this;
            }

            return null;
        }

        #endregion


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