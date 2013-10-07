using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace fNbt {
    public sealed class NbtWriter {
        public int Depth { get; private set; }

        readonly BinaryWriter writer;
        NbtTagType listType;
        NbtTagType parentType;
        int listIndex;
        int listSize;
        bool done;
        Stack<NbtWriterNode> nodes;


        public NbtWriter( [NotNull] Stream stream, [NotNull] String rootTagName )
            : this( stream, rootTagName, true ) {}


        public NbtWriter( [NotNull] Stream stream, [NotNull] String rootTagName, bool bigEndian ) {
            if( rootTagName == null )
                throw new ArgumentNullException( "rootTagName" );
            writer = new NbtBinaryWriter( stream, bigEndian );
            writer.Write( (byte)NbtTagType.Compound );
            writer.Write( rootTagName );
            parentType = NbtTagType.Compound;
            Depth = 1;
        }


        void GoDown( NbtTagType thisType ) {
            if( nodes == null ) {
                nodes = new Stack<NbtWriterNode>();
            }
            NbtWriterNode newNode = new NbtWriterNode {
                ParentType = parentType,
                ListType = listType,
                ListSize = listSize,
                ListIndex = listIndex
            };
            nodes.Push( newNode );
            Depth++;

            parentType = thisType;
            listType = NbtTagType.Unknown;
            listSize = 0;
            listIndex = 0;
        }


        void GoUp() {
            if( nodes == null || nodes.Count == 0 ) {
                done = true;
            } else {
                NbtWriterNode oldNode = nodes.Pop();
                parentType = oldNode.ParentType;
                listType = oldNode.ListType;
                listSize = oldNode.ListSize;
                listIndex = oldNode.ListIndex;
            }
            Depth--;
        }


        #region Compounds and Lists

        public void BeginCompound() {
            EnforceConstraints( null, NbtTagType.Compound );
            GoDown( NbtTagType.Compound );
        }


        public void BeginCompound( [NotNull] String name ) {
            EnforceConstraints( name, NbtTagType.Compound );
            GoDown( NbtTagType.Compound );

            writer.Write( (byte)NbtTagType.Compound );
            writer.Write( name );
        }


        public void EndCompound() {
            if( parentType != NbtTagType.Compound || done ) {
                throw new NbtFormatException( "Not currently in a compound." );
            }
            GoUp();
            writer.Write( (byte)NbtTagType.End );
        }


        public void BeginList( NbtTagType type, int size ) {
            if( size < 0 ) {
                throw new ArgumentOutOfRangeException( "size", "List size may not be negative." );
            }
            EnforceConstraints( null, NbtTagType.List );
            GoDown( NbtTagType.List );
            listType = type;
            listSize = size;

            writer.Write( (byte)type );
            writer.Write( size );
        }


        public void BeginList( [NotNull] String tagName, NbtTagType type, int size ) {
            if( size < 0 ) {
                throw new ArgumentOutOfRangeException( "size", "List size may not be negative." );
            }
            EnforceConstraints( tagName, NbtTagType.List );
            GoDown( NbtTagType.List );
            listType = type;
            listSize = size;

            writer.Write( (byte)NbtTagType.List );
            writer.Write( tagName );
            writer.Write( (byte)type );
            writer.Write( size );
        }


        public void EndList() {
            if( parentType != NbtTagType.List || done ) {
                throw new NbtFormatException( "Not currently in a list." );
            } else if( listIndex < listSize ) {
                throw new NbtFormatException( "Cannot end list: not all list elements have been written yet. " +
                                              "Expected: " + listSize + ", written: " + listIndex );
            }
            GoUp();
        }

        #endregion


        #region Value Tags

        public void WriteByte( byte value ) {
            EnforceConstraints( null, NbtTagType.Byte );
            writer.Write( value );
        }


        public void WriteByte( [NotNull] String tagName, byte value ) {
            EnforceConstraints( tagName, NbtTagType.Byte );
            writer.Write( (byte)NbtTagType.Byte );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteByteArray( [NotNull] byte[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( null, NbtTagType.ByteArray );
            writer.Write( value.Length );
            writer.Write( value, 0, value.Length );
        }


        public void WriteByteArray( [NotNull] String tagName, [NotNull] byte[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( tagName, NbtTagType.ByteArray );
            writer.Write( (byte)NbtTagType.ByteArray );
            writer.Write( tagName );
            writer.Write( value.Length );
            writer.Write( value, 0, value.Length );
        }


        public void WriteDouble( double value ) {
            EnforceConstraints( null, NbtTagType.Double );
            writer.Write( value );
        }


        public void WriteDouble( [NotNull] String tagName, double value ) {
            EnforceConstraints( tagName, NbtTagType.Double );
            writer.Write( (byte)NbtTagType.Double );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteFloat( float value ) {
            EnforceConstraints( null, NbtTagType.Float );
            writer.Write( value );
        }


        public void WriteFloat( [NotNull] String tagName, float value ) {
            EnforceConstraints( tagName, NbtTagType.Float );
            writer.Write( (byte)NbtTagType.Float );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteInt( int value ) {
            EnforceConstraints( null, NbtTagType.Int );
            writer.Write( value );
        }


        public void WriteInt( [NotNull] String tagName, int value ) {
            EnforceConstraints( tagName, NbtTagType.Int );
            writer.Write( (byte)NbtTagType.Int );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteIntArray( [NotNull] int[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( null, NbtTagType.IntArray );
            writer.Write( value.Length );
            for( int i = 0; i < value.Length; i++ ) {
                writer.Write( value[i] );
            }
        }


        public void WriteIntArray( [NotNull] String tagName, [NotNull] int[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( tagName, NbtTagType.IntArray );
            writer.Write( (byte)NbtTagType.IntArray );
            writer.Write( tagName );
            writer.Write( value.Length );
            for( int i = 0; i < value.Length; i++ ) {
                writer.Write( value[i] );
            }
        }


        public void WriteLong( long value ) {
            EnforceConstraints( null, NbtTagType.Long );
            writer.Write( value );
        }


        public void WriteLong( [NotNull] String tagName, long value ) {
            EnforceConstraints( tagName, NbtTagType.Long );
            writer.Write( (byte)NbtTagType.Long );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteShort( short value ) {
            EnforceConstraints( null, NbtTagType.Short );
            writer.Write( value );
        }


        public void WriteShort( [NotNull] String tagName, short value ) {
            EnforceConstraints( tagName, NbtTagType.Short );
            writer.Write( (byte)NbtTagType.Short );
            writer.Write( tagName );
            writer.Write( value );
        }


        public void WriteString( [NotNull] String value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( null, NbtTagType.String );
            writer.Write( value );
        }


        public void WriteString( [NotNull] String tagName, [NotNull] String value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            EnforceConstraints( tagName, NbtTagType.String );
            writer.Write( (byte)NbtTagType.String );
            writer.Write( tagName );
            writer.Write( value );
        }

        #endregion


        public void WriteTag( [NotNull] NbtTag tag ) {
            if( tag == null )
                throw new ArgumentNullException( "tag" );
            EnforceConstraints( tag.Name, tag.TagType );
            if( tag.Name != null ) {
                tag.WriteTag( (NbtBinaryWriter)writer );
            } else {
                tag.WriteData( (NbtBinaryWriter)writer );
            }
        }


        public void Finish() {
            if( !done ) {
                throw new NbtFormatException( "Cannot finish: not all tags have been closed yet." );
            }
        }


        void EnforceConstraints( [CanBeNull] String name, NbtTagType desiredType ) {
            if( done ) {
                throw new NbtFormatException( "Cannot write any more tags: root tag has been closed." );
            }
            if( parentType == NbtTagType.List ) {
                if( name != null ) {
                    throw new NbtFormatException( "Expecting an unnamed tag." );
                } else if( listType != desiredType ) {
                    throw new NbtFormatException( "Unexpected tag type (expected: " + listType + ", given: " +
                                                  desiredType );
                } else if( listIndex >= listSize ) {
                    throw new NbtFormatException( "Given list size exceeded." );
                }
                listIndex++;
            } else if( name == null ) {
                throw new NbtFormatException( "Expecting a named tag." );
            }
        }
    }
}