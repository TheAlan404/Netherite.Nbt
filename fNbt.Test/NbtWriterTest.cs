using System;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    class NbtWriterTest {
        [Test]
        public void ValueTest() {
            using( MemoryStream ms = new MemoryStream() ) {
                NbtWriter writer = new NbtWriter( ms, "root" ); {
                    writer.WriteByte( "byte", 1 );
                    writer.WriteShort( "short", 2 );
                    writer.WriteInt( "int", 3 );
                    writer.WriteLong( "long", 4L );
                    writer.WriteFloat( "float", 5f );
                    writer.WriteDouble( "double", 6d );
                    writer.WriteByteArray( "byteArray", new byte[] { 10, 11, 12 } );
                    writer.WriteIntArray( "intArray", new[]{ 20, 21, 22 } );
                    writer.WriteString( "string", "123" );
                } writer.EndCompound();
                writer.Finish();

                ms.Seek( 0, SeekOrigin.Begin );
                NbtFile file = new NbtFile();
                file.LoadFromStream( ms, NbtCompression.None );

                TestFiles.AssertValueTest( file );
            }
        }


        [Test]
        public void CompoundListTest() {
            // test various combinations of compound tags and list tags
            const string testString = "Come on and slam, and welcome to the jam.";
            using( MemoryStream ms = new MemoryStream() ) {
                NbtWriter writer = new NbtWriter( ms, "Test" ); {
                    writer.BeginCompound( "EmptyCompy" ); {
                    } writer.EndCompound();

                    writer.BeginCompound( "OuterNestedCompy" ); {
                        writer.BeginCompound( "InnerNestedCompy" ); {
                            writer.WriteInt( "IntTest", 123 );
                            writer.WriteString( "StringTest", testString );
                        } writer.EndCompound();
                    } writer.EndCompound();

                    writer.BeginList( "ListOfInts", NbtTagType.Int, 3 ); {
                        writer.WriteInt( 1 );
                        writer.WriteInt( 2 );
                        writer.WriteInt( 3 );
                    } writer.EndList();

                    writer.BeginCompound( "CompoundOfListsOfCompounds" ); {
                        writer.BeginList( "ListOfCompounds", NbtTagType.Compound, 1 ); {
                            writer.BeginCompound(); {
                                writer.WriteInt( "TestInt", 123 );
                            } writer.EndCompound();
                        } writer.EndList();
                    } writer.EndCompound();


                    writer.BeginList( "ListOfEmptyLists", NbtTagType.List, 3 ); {
                        writer.BeginList( NbtTagType.List, 0 ); {
                        } writer.EndList();
                        writer.BeginList( NbtTagType.List, 0 ); {
                        } writer.EndList();
                        writer.BeginList( NbtTagType.List, 0 ); {
                        } writer.EndList();
                    } writer.EndList();
                } writer.EndCompound();
                writer.Finish();

                ms.Seek( 0, SeekOrigin.Begin );
                NbtFile file = new NbtFile();
                file.LoadFromStream( ms, NbtCompression.None );
                Console.WriteLine( file.ToString() );
            }
        }


        [Test]
        public void ListTest() {
            // write short (1-element) lists of every possible kind
            using( MemoryStream ms = new MemoryStream() ) {
                NbtWriter writer = new NbtWriter( ms, "Test" );
                writer.BeginList( "LotsOfLists", NbtTagType.List, 11 ); {

                    writer.BeginList( NbtTagType.Byte, 1 );
                    writer.WriteByte( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.ByteArray, 1 );
                    writer.WriteByteArray( new byte[] {
                        1
                    } );
                    writer.EndList();

                    writer.BeginList( NbtTagType.Compound, 1 );
                    writer.BeginCompound();
                    writer.EndCompound();
                    writer.EndList();

                    writer.BeginList( NbtTagType.Double, 1 );
                    writer.WriteDouble( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.Float, 1 );
                    writer.WriteFloat( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.Int, 1 );
                    writer.WriteInt( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.IntArray, 1 );
                    writer.WriteIntArray( new[] {
                        1
                    } );
                    writer.EndList();

                    writer.BeginList( NbtTagType.List, 1 );
                    writer.BeginList( NbtTagType.List, 0 );
                    writer.EndList();
                    writer.EndList();

                    writer.BeginList( NbtTagType.Long, 1 );
                    writer.WriteLong( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.Short, 1 );
                    writer.WriteShort( 1 );
                    writer.EndList();

                    writer.BeginList( NbtTagType.String, 1 );
                    writer.WriteString( "ponies" );
                    writer.EndList();

                } writer.EndList();
                writer.EndCompound();
                writer.Finish();

                ms.Position = 0;
                NbtReader reader = new NbtReader( ms );
                NbtTag root = reader.ReadAsTag();
            }
        }


        [Test]
        public void WriteTagTest() {
            using( MemoryStream ms = new MemoryStream() ) {
                NbtWriter writer = new NbtWriter( ms, "root" ); {
                    foreach( NbtTag tag in TestFiles.MakeValueTest().Tags ) {
                        writer.WriteTag( tag );
                    }
                    writer.EndCompound();
                    writer.Finish();
                }
                ms.Position = 0;
                NbtFile file = new NbtFile();
                file.LoadFromBuffer( ms.ToArray(), 0, (int)ms.Length, NbtCompression.None );
                TestFiles.AssertValueTest( file );
            }
        }


        [Test]
        public void ErrorTest() {
            using( MemoryStream ms = new MemoryStream() ) {

                // null stream or root name
                Assert.Throws<ArgumentNullException>( () => new NbtWriter( null, "root" ) );
                Assert.Throws<ArgumentNullException>( () => new NbtWriter( ms, null ) );

                NbtWriter writer = new NbtWriter( ms, "root" ); {
                    // use negative list size
                    Assert.Throws<ArgumentOutOfRangeException>( () => writer.BeginList( "list", NbtTagType.Int, -1 ) );
                    writer.BeginList( "listOfLists", NbtTagType.List, 1 );
                    Assert.Throws<ArgumentOutOfRangeException>( () => writer.BeginList( NbtTagType.Int, -1 ) );
                    writer.BeginList( NbtTagType.Int, 0 );
                    writer.EndList();
                    writer.EndList();

                    writer.BeginList( "list", NbtTagType.Int, 1 );

                    // call EndCompound when not in a compound
                    Assert.Throws<NbtFormatException>( writer.EndCompound );

                    // end list before all elements have been written
                    Assert.Throws<NbtFormatException>( writer.EndList );

                    // write the wrong kind of tag inside a list
                    Assert.Throws<NbtFormatException>( () => writer.WriteShort( 0 ) );

                    // write a named tag where an unnamed tag is expected
                    Assert.Throws<NbtFormatException>( () => writer.WriteInt( "NamedInt", 0 ) );

                    // write too many list elements
                    writer.WriteTag( new NbtInt() );
                    Assert.Throws<NbtFormatException>( () => writer.WriteInt( 0 ) );
                    writer.EndList();

                    // write a null tag
                    Assert.Throws<ArgumentNullException>( () => writer.WriteTag( null ) );

                    // write an unnamed tag where a named tag is expected
                    Assert.Throws<NbtFormatException>( () => writer.WriteTag( new NbtInt() ) );
                    Assert.Throws<NbtFormatException>( () => writer.WriteInt(0) );

                    // end a list when not in a list
                    Assert.Throws<NbtFormatException>( writer.EndList );

                    // write null values where unacceptable
                    Assert.Throws<ArgumentNullException>( () => writer.WriteString( "NullString", null ) );
                    Assert.Throws<ArgumentNullException>( () => writer.WriteByteArray( "NullByteArray", null ) );
                    Assert.Throws<ArgumentNullException>( () => writer.WriteIntArray( "NullIntArray", null ) );
                    Assert.Throws<ArgumentNullException>( () => writer.WriteString( null ) );
                    Assert.Throws<ArgumentNullException>( () => writer.WriteByteArray( null ) );
                    Assert.Throws<ArgumentNullException>( () => writer.WriteIntArray( null ) );

                    // finish too early
                    Assert.Throws<NbtFormatException>( writer.Finish );

                    writer.EndCompound();
                    writer.Finish();

                    // write tag after finishing
                    Assert.Throws<NbtFormatException>( () => writer.WriteTag( new NbtInt() ) );
                }
            }
        }
    }
}
