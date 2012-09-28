using System;
using System.Collections.Generic;
using System.Text;

namespace LibNbt.Queries {
    public sealed class TagQuery {
        public string Query { get; protected set; }
        readonly List<TagQueryToken> tokens;
        int currentTokenIndex;

        public TagQuery() : this( "" ) {}


        public TagQuery( string query ) {
            tokens = new List<TagQueryToken>();

            if( !string.IsNullOrEmpty( query ) ) {
                SetQuery( query );
            }
        }


        public void SetQuery( string query ) {
            Query = query;

            if( string.IsNullOrEmpty( Query ) ) {
                throw new ArgumentException( "You must provide a query.", "query" );
            }
            if( !Query.StartsWith( "/" ) ) {
                throw new ArgumentException( "The query must begin with a \"/\".", "query" );
            }

            tokens.Clear();
            currentTokenIndex = -1;

            var escapingChar = false;
            TagQueryToken token = null;
            var sbToken = new StringBuilder();
            for( var i = 0; i < query.Length; i++ ) {
                if( escapingChar ) {
                    sbToken.Append( query[i] );
                    escapingChar = false;
                    continue;
                }

                if( query[i] == '/' ) {
                    if( token != null ) {
                        token.Name = sbToken.ToString();
                        tokens.Add( token );
                    }

                    token = new TagQueryToken { Query = this };
                    sbToken.Length = 0;
                    continue;
                }

                if( query[i] == '\\' ) {
                    escapingChar = true;
                    continue;
                }

                sbToken.Append( query[i] );
            }
            if( token != null ) {
                token.Name = sbToken.ToString();
                tokens.Add( token );
            }
        }


        /// <summary>
        /// The total number of tokens in the query.
        /// </summary>
        /// <returns>The number of tokens</returns>
        public int Count() {
            return tokens.Count;
        }


        /// <summary>
        /// The number of tokens left in the query after the current one.
        /// </summary>
        /// <returns>The number of tokens</returns>
        public int TokensLeft() {
            return Count() - ( currentTokenIndex + 1 );
        }


        public void MoveFirst() {
            currentTokenIndex = -1;
        }


        public TagQueryToken Previous() {
            if( currentTokenIndex >= 0 ) {
                return tokens[--currentTokenIndex];
            }
            return null;
        }


        public TagQueryToken Next() {
            if( currentTokenIndex + 1 < Count() ) {
                return tokens[++currentTokenIndex];
            }
            return null;
        }


        public TagQueryToken Peek() {
            if( currentTokenIndex + 1 < Count() ) {
                return tokens[currentTokenIndex + 1];
            }
            return null;
        }
    }
}