using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class ListTests {
        const string TempDir = "TestTemp";


        public static NbtCompound MakeListTest() {
            return new NbtCompound( "Root" ) {
                new NbtList( "ByteList" ) {
                    new NbtByte( 100 ),
                    new NbtByte( 20 ),
                    new NbtByte( 3 )
                },
                new NbtList( "DoubleList" ) {
                    new NbtDouble( 1d ),
                    new NbtDouble( 2000d ),
                    new NbtDouble( -3000000d )
                },
                new NbtList( "FloatList" ) {
                    new NbtFloat( 1f ),
                    new NbtFloat( 2000f ),
                    new NbtFloat( -3000000f )
                },
                new NbtList( "IntList" ) {
                    new NbtInt( 1 ),
                    new NbtInt( 2000 ),
                    new NbtInt( -3000000 )
                },
                new NbtList( "LongList" ) {
                    new NbtLong( 1L ),
                    new NbtLong( 2000L ),
                    new NbtLong( -3000000L )
                },
                new NbtList( "ShortList" ) {
                    new NbtShort( 1 ),
                    new NbtShort( 200 ),
                    new NbtShort( -30000 )
                },
                new NbtList( "StringList" ) {
                    new NbtString( "one" ),
                    new NbtString( "two thousand" ),
                    new NbtString( "negative three million" )
                },
                new NbtList( "CompoundList" ) {
                    new NbtCompound(),
                    new NbtCompound(),
                    new NbtCompound()
                },
                new NbtList( "ListList" ) {
                    new NbtList( NbtTagType.List ),
                    new NbtList( NbtTagType.List ),
                    new NbtList( NbtTagType.List )
                },
                new NbtList( "ByteArrayList" ) {
                    new NbtByteArray( new byte[] {
                        1, 2, 3
                    } ),
                    new NbtByteArray( new byte[] {
                        11, 12, 13
                    } ),
                    new NbtByteArray( new byte[] {
                        21, 22, 23
                    } )
                },
                new NbtList( "IntArrayList" ) {
                    new NbtIntArray( new[] {
                        1, -2, 3
                    } ),
                    new NbtIntArray( new[] {
                        1000, -2000, 3000
                    } ),
                    new NbtIntArray( new[] {
                        1000000, -2000000, 3000000
                    } )
                }
            };
        }


        [SetUp]
        public void ListTestsSetup() {
            Directory.CreateDirectory( TempDir );
        }


        [Test]
        public void InterfaceImplementation() {
            List<NbtTag> referenceList = new List<NbtTag>();
            referenceList.Add( new NbtInt(1) );
            referenceList.Add( new NbtInt(2) );
            referenceList.Add( new NbtInt(3) );
            NbtInt testTag = new NbtInt(4);

            NbtList originalList = new NbtList( referenceList );
            IList iList = originalList;
            CollectionAssert.AreEqual( referenceList, iList );

            referenceList.Add( testTag );
            iList.Add( testTag );
            CollectionAssert.AreEqual( referenceList, iList );
            Assert.AreEqual( referenceList.IndexOf( testTag ), iList.IndexOf( testTag ) );
            Assert.IsTrue( iList.Contains( testTag ) );
            iList.Remove( testTag );
            Assert.IsFalse( iList.Contains( testTag ) );
            iList.Insert( 0, testTag );
            Assert.AreEqual( iList.IndexOf( testTag ), 0 );
            iList.RemoveAt( 0 );
            Assert.IsFalse( iList.Contains( testTag ) );

            Assert.IsFalse( iList.IsFixedSize );
            Assert.IsFalse( iList.IsReadOnly );
            Assert.IsFalse( iList.IsSynchronized );
            Assert.NotNull( iList.SyncRoot );

            NbtInt[] exportTest = new NbtInt[iList.Count];
            iList.CopyTo( exportTest, 0 );
            CollectionAssert.AreEqual( iList, exportTest );
            for( int i = 0; i < iList.Count; i++ ) {
                Assert.AreEqual( iList[i], originalList[i] );
            }

            IList<NbtTag> iGenericList = originalList;
            Assert.IsFalse( iGenericList.IsReadOnly );

            iList.Clear();
            Assert.AreEqual( iList.Count, 0 );
            Assert.AreEqual( iList.IndexOf( testTag ), -1 );
        }


        [Test]
        public void InitializingListFromCollection() {
            // auto-detecting list type
            Assert.DoesNotThrow( () => new NbtList( "Test1", new NbtTag[] {
                new NbtInt( 1 ),
                new NbtInt( 2 ),
                new NbtInt( 3 )
            } ) );

            Assert.AreEqual( new NbtList( "Test1", new NbtTag[] {
                new NbtInt( 1 ),
                new NbtInt( 2 ),
                new NbtInt( 3 )
            } ).ListType, NbtTagType.Int );

            // correct explicitly-given list type
            Assert.DoesNotThrow( () => new NbtList( "Test2", new NbtTag[] {
                new NbtInt( 1 ),
                new NbtInt( 2 ),
                new NbtInt( 3 )
            }, NbtTagType.Int ) );

            // wrong explicitly-given list type
            Assert.Throws<ArgumentException>( () => new NbtList( "Test3", new NbtTag[] {
                new NbtInt( 1 ),
                new NbtInt( 2 ),
                new NbtInt( 3 )
            }, NbtTagType.Float ) );

            // auto-detecting mixed list given
            Assert.Throws<ArgumentException>( () => new NbtList( "Test4", new NbtTag[] {
                new NbtFloat( 1 ),
                new NbtByte( 2 ),
                new NbtInt( 3 )
            } ) );
        }


        [Test]
        public void ManipulatingList() {
            var sameTags = new NbtTag[] {
                new NbtInt( 0 ),
                new NbtInt( 1 ),
                new NbtInt( 2 )
            };

            NbtList list = new NbtList( "Test1", sameTags );

            // testing enumerator
            int j = 0;
            foreach( NbtTag tag in list ) {
                Assert.AreEqual( tag, sameTags[j++] );
            }

            // adding an item of correct type
            list.Add( new NbtInt( 3 ) );
            list.Insert( 3, new NbtInt( 4 ) );

            // adding an item of wrong type
            Assert.Throws<ArgumentException>( () => list.Add( new NbtString() ) );
            Assert.Throws<ArgumentException>( () => list.Insert( 3, new NbtString() ) );

            // testing array contents
            for( int i = 0; i < sameTags.Length; i++ ) {
                Assert.AreSame( sameTags[i], list[i] );
                Assert.AreEqual( ( (NbtInt)list[i] ).Value, i );
            }

            // test removal
            Assert.IsFalse( list.Remove( new NbtInt( 5 ) ) );
            Assert.IsTrue( list.Remove( sameTags[0] ) );
            list.RemoveAt( 0 );
            Assert.Throws<ArgumentOutOfRangeException>( () => list.RemoveAt( 10 ) );
        }


        [Test]
        public void ChangingListTagType() {
            var list = new NbtList();

            // changing list type to an out-of-range type
            Assert.Throws<ArgumentOutOfRangeException>( () => list.ListType = (NbtTagType)200 );

            // changing type of an empty list
            Assert.DoesNotThrow( () => list.ListType = NbtTagType.Unknown );

            list.Add( new NbtInt() );

            // setting correct type for a non-empty list
            Assert.DoesNotThrow( () => list.ListType = NbtTagType.Int );

            // changing list type to an incorrect type
            Assert.Throws<ArgumentException>( () => list.ListType = NbtTagType.Short );
        }


        [Test]
        public void SerializingWithoutListType() {
            NbtCompound root = new NbtCompound( "root" ) { new NbtList( "list" ) };
            NbtFile file = new NbtFile( root );

            using( MemoryStream ms = new MemoryStream() ) {
                // list should throw NbtFormatException, because its ListType is Unknown
                Assert.Throws<NbtFormatException>( () => file.SaveToStream( ms, NbtCompression.None ) );
            }
        }


        [Test]
        public void Serializing1() {
            string fileName = Path.Combine( TempDir, "NbtListType.nbt" );
            const NbtTagType expectedListType = NbtTagType.Int;
            const int elements = 10;

            // construct nbt file
            NbtFile writtenFile = new NbtFile( new NbtCompound( "ListTypeTest" ) );
            NbtList writtenList = new NbtList( "Entities", null, expectedListType );
            for( int i = 0; i < elements; i++ ) {
                writtenList.Add( new NbtInt( i ) );
            }
            writtenFile.RootTag.Add( writtenList );

            // test saving
            writtenFile.SaveToFile( fileName, NbtCompression.GZip );

            // test loading
            NbtFile readFile = new NbtFile( fileName );

            // check contents of loaded file
            Assert.NotNull( readFile.RootTag );
            Assert.IsInstanceOf<NbtList>( readFile.RootTag["Entities"] );
            NbtList readList = (NbtList)readFile.RootTag["Entities"];
            Assert.AreEqual( readList.ListType, writtenList.ListType );
            Assert.AreEqual( readList.Count, writtenList.Count );

            // check .ToArray
            CollectionAssert.AreEquivalent( readList, readList.ToArray() );
            CollectionAssert.AreEquivalent( readList, readList.ToArray<NbtInt>() );

            // check contents of loaded list
            for( int i = 0; i < elements; i++ ) {
                Assert.AreEqual( readList.Get<NbtInt>( i ).Value, writtenList.Get<NbtInt>( i ).Value );
            }
        }


        [Test]
        public void Serializing2() {
            NbtFile testFile = new NbtFile( MakeListTest() );
            byte[] buffer = testFile.SaveToBuffer( NbtCompression.None );
            testFile.LoadFromBuffer( buffer, 0, buffer.Length, NbtCompression.None );
        }


        [Test]
        public void NestedListAndCompoundTest() {
            byte[] data;
            {
                NbtCompound root = new NbtCompound( "Root" );
                NbtList outerList = new NbtList( "OuterList", NbtTagType.Compound );
                NbtCompound outerCompound = new NbtCompound();
                NbtList innerList = new NbtList( "InnerList", NbtTagType.Compound );
                NbtCompound innerCompound = new NbtCompound();

                innerList.Add( innerCompound );
                outerCompound.Add( innerList );
                outerList.Add( outerCompound );
                root.Add( outerList );

                NbtFile file = new NbtFile( root );
                data = file.SaveToBuffer( NbtCompression.None );
            }
            {
                NbtFile file = new NbtFile();
                file.LoadFromBuffer( data, 0, data.Length, NbtCompression.None );
                Assert.AreEqual( file.RootTag.Get<NbtList>( "OuterList" ).Count, 1 );
                Assert.AreEqual( file.RootTag.Get<NbtList>( "OuterList" ).Get<NbtCompound>( 0 ).Name, null );
                Assert.AreEqual(
                    file.RootTag.Get<NbtList>( "OuterList" ).Get<NbtCompound>( 0 ).Get<NbtList>( "InnerList" ).Count,
                    1 );
                Assert.AreEqual(
                    file.RootTag.Get<NbtList>( "OuterList" )
                        .Get<NbtCompound>( 0 )
                        .Get<NbtList>( "InnerList" )
                        .Get<NbtCompound>( 0 )
                        .Name,
                    null );
            }
        }


        [TearDown]
        public void ListTestsTearDown() {
            if( Directory.Exists( TempDir ) ) {
                foreach( var file in Directory.GetFiles( TempDir ) ) {
                    File.Delete( file );
                }
                Directory.Delete( TempDir );
            }
        }
    }
}