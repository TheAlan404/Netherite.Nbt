using System;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class NbtReaderTests {
        [Test]
        public void PrintBigFileUncompressed() {
            using( FileStream fs = File.OpenRead( "TestFiles/bigtest.nbt" ) ) {
                NbtReader reader = new NbtReader( fs );
                Assert.AreEqual( reader.BaseStream, fs );
                while( reader.ReadToFollowing() ) {
                    Console.Write( "@" + reader.TagStartOffset + " " );
                    Console.WriteLine( reader.ToString() );
                }
                Assert.AreEqual( reader.RootName, "Level" );
            }
        }


        [Test]
        public void CacheTagValuesTest() {
            byte[] testData = new NbtFile( TestFiles.MakeValueTest() ).SaveToBuffer( NbtCompression.None );
            NbtReader reader = new NbtReader( new MemoryStream( testData ) );
            Assert.IsFalse( reader.CacheTagValues );
            reader.CacheTagValues = true;
            Assert.IsTrue( reader.ReadToFollowing() ); // root

            Assert.IsTrue( reader.ReadToFollowing() ); // byte
            Assert.AreEqual( reader.ReadValue(), 1 );
            Assert.AreEqual( reader.ReadValue(), 1 );
            Assert.IsTrue( reader.ReadToFollowing() ); // short
            Assert.AreEqual( reader.ReadValue(), 2 );
            Assert.AreEqual( reader.ReadValue(), 2 );
            Assert.IsTrue( reader.ReadToFollowing() ); // int
            Assert.AreEqual( reader.ReadValue(), 3 );
            Assert.AreEqual( reader.ReadValue(), 3 );
            Assert.IsTrue( reader.ReadToFollowing() ); // long
            Assert.AreEqual( reader.ReadValue(), 4L );
            Assert.AreEqual( reader.ReadValue(), 4L );
            Assert.IsTrue( reader.ReadToFollowing() ); // float
            Assert.AreEqual( reader.ReadValue(), 5f );
            Assert.AreEqual( reader.ReadValue(), 5f );
            Assert.IsTrue( reader.ReadToFollowing() ); // double
            Assert.AreEqual( reader.ReadValue(), 6d );
            Assert.AreEqual( reader.ReadValue(), 6d );
            Assert.IsTrue( reader.ReadToFollowing() ); // byteArray
            CollectionAssert.AreEqual( (byte[])reader.ReadValue(), new byte[] { 10, 11, 12 } );
            CollectionAssert.AreEqual( (byte[])reader.ReadValue(), new byte[] { 10, 11, 12 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // intArray
            CollectionAssert.AreEqual( (int[])reader.ReadValue(), new[] { 20, 21, 22 } );
            CollectionAssert.AreEqual( (int[])reader.ReadValue(), new[] { 20, 21, 22 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // string
            Assert.AreEqual( reader.ReadValue(), "123" );
            Assert.AreEqual( reader.ReadValue(), "123" );
        }


        [Test]
        public void NestedListTest() {
            NbtCompound root = new NbtCompound( "root" ) {
                new NbtList( "OuterList" ) {
                    new NbtList {
                        new NbtByte()
                    },
                    new NbtList {
                        new NbtShort()
                    },
                    new NbtList {
                        new NbtInt()
                    }
                }
            };
            byte[] testData = new NbtFile( root ).SaveToBuffer( NbtCompression.None );
            using( MemoryStream ms = new MemoryStream( testData ) ) {
                NbtReader reader = new NbtReader( ms );
                while( reader.ReadToFollowing() ) {
                    Console.WriteLine( reader.ToString( true ) );
                }
            }
        }


        [Test]
        public void PropertiesTest() {
            NbtReader reader = new NbtReader( TestFiles.MakeTest() );
            Assert.AreEqual( reader.Depth, 0 );
            Assert.AreEqual( reader.TagsRead, 0 );

            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "root" );
            Assert.AreEqual( reader.TagType, NbtTagType.Compound );
            Assert.AreEqual( reader.ListType, NbtTagType.Unknown );
            Assert.IsFalse( reader.HasValue );
            Assert.IsTrue( reader.IsCompound );
            Assert.IsFalse( reader.IsList );
            Assert.IsFalse( reader.IsListElement );
            Assert.IsFalse( reader.HasLength );
            Assert.AreEqual( reader.ListIndex, 0 );
            Assert.AreEqual( reader.Depth, 1 );
            Assert.AreEqual( reader.ParentName, null );
            Assert.AreEqual( reader.ParentTagType, NbtTagType.Unknown );
            Assert.AreEqual( reader.ParentTagLength, 0 );
            Assert.AreEqual( reader.TagLength, 0 );
            Assert.AreEqual( reader.TagsRead, 1 );

            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "first" );
            Assert.AreEqual( reader.TagType, NbtTagType.Int );
            Assert.AreEqual( reader.ListType, NbtTagType.Unknown );
            Assert.IsTrue( reader.HasValue );
            Assert.IsFalse( reader.IsCompound );
            Assert.IsFalse( reader.IsList );
            Assert.IsFalse( reader.IsListElement );
            Assert.IsFalse( reader.HasLength );
            Assert.AreEqual( reader.ListIndex, 0 );
            Assert.AreEqual( reader.Depth, 2 );
            Assert.AreEqual( reader.ParentName, "root" );
            Assert.AreEqual( reader.ParentTagType, NbtTagType.Compound );
            Assert.AreEqual( reader.ParentTagLength, 0 );
            Assert.AreEqual( reader.TagLength, 0 );
            Assert.AreEqual( reader.TagsRead, 2 );

            Assert.IsTrue( reader.ReadToFollowing( "fourth-list" ) );
            Assert.AreEqual( reader.TagName, "fourth-list" );
            Assert.AreEqual( reader.TagType, NbtTagType.List );
            Assert.AreEqual( reader.ListType, NbtTagType.List );
            Assert.IsFalse( reader.HasValue );
            Assert.IsFalse( reader.IsCompound );
            Assert.IsTrue( reader.IsList );
            Assert.IsFalse( reader.IsListElement );
            Assert.IsTrue( reader.HasLength );
            Assert.AreEqual( reader.ListIndex, 0 );
            Assert.AreEqual( reader.Depth, 2 );
            Assert.AreEqual( reader.ParentName, "root" );
            Assert.AreEqual( reader.ParentTagType, NbtTagType.Compound );
            Assert.AreEqual( reader.ParentTagLength, 0 );
            Assert.AreEqual( reader.TagLength, 3 );
            Assert.AreEqual( reader.TagsRead, 8 );

            Assert.IsTrue( reader.ReadToFollowing() ); // first list element, itself a list
            Assert.AreEqual( reader.TagName, null );
            Assert.AreEqual( reader.TagType, NbtTagType.List );
            Assert.AreEqual( reader.ListType, NbtTagType.Compound );
            Assert.IsFalse( reader.HasValue );
            Assert.IsFalse( reader.IsCompound );
            Assert.IsTrue( reader.IsList );
            Assert.IsTrue( reader.IsListElement );
            Assert.IsTrue( reader.HasLength );
            Assert.AreEqual( reader.ListIndex, 0 );
            Assert.AreEqual( reader.Depth, 3 );
            Assert.AreEqual( reader.ParentName, "fourth-list" );
            Assert.AreEqual( reader.ParentTagType, NbtTagType.List );
            Assert.AreEqual( reader.ParentTagLength, 3 );
            Assert.AreEqual( reader.TagLength, 1 );
            Assert.AreEqual( reader.TagsRead, 9 );

            Assert.IsTrue( reader.ReadToFollowing() ); // first nested list element, compound
            Assert.AreEqual( reader.TagName, null );
            Assert.AreEqual( reader.TagType, NbtTagType.Compound );
            Assert.AreEqual( reader.ListType, NbtTagType.Unknown );
            Assert.IsFalse( reader.HasValue );
            Assert.IsTrue( reader.IsCompound );
            Assert.IsFalse( reader.IsList );
            Assert.IsTrue( reader.IsListElement );
            Assert.IsFalse( reader.HasLength );
            Assert.AreEqual( reader.ListIndex, 0 );
            Assert.AreEqual( reader.Depth, 4 );
            Assert.AreEqual( reader.ParentName, null );
            Assert.AreEqual( reader.ParentTagType, NbtTagType.List );
            Assert.AreEqual( reader.ParentTagLength, 1 );
            Assert.AreEqual( reader.TagLength, 0 );
            Assert.AreEqual( reader.TagsRead, 10 );
        }


        [Test]
        public void ReadToSiblingTest() {
            NbtReader reader = new NbtReader( TestFiles.MakeTest() );
            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "root" );
            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "first" );
            Assert.IsTrue( reader.ReadToNextSibling( "third-comp" ) );
            Assert.AreEqual( reader.TagName, "third-comp" );
            Assert.IsTrue( reader.ReadToNextSibling() );
            Assert.AreEqual( reader.TagName, "fourth-list" );
            Assert.IsTrue( reader.ReadToNextSibling() );
            Assert.AreEqual( reader.TagName, "fifth" );
            Assert.IsFalse( reader.ReadToNextSibling() );
        }


        [Test]
        public void ReadToDescendantTest() {
            NbtReader reader = new NbtReader( TestFiles.MakeTest() );
            Assert.IsTrue( reader.ReadToDescendant( "third-comp" ) );
            Assert.AreEqual( reader.TagName, "third-comp" );
            Assert.IsTrue( reader.ReadToDescendant( "inComp2" ) );
            Assert.AreEqual( reader.TagName, "inComp2" );
            Assert.IsFalse( reader.ReadToDescendant( "derp" ) );
            Assert.AreEqual( reader.TagName, "inComp3" );
            reader.ReadToFollowing(); // at fourth-list
            Assert.IsTrue( reader.ReadToDescendant( "inList2" ) );
            Assert.AreEqual( reader.TagName, "inList2" );
        }


        [Test]
        public void SkipTest() {
            NbtReader reader = new NbtReader( TestFiles.MakeTest() );
            reader.ReadToFollowing(); // at root
            reader.ReadToFollowing(); // at first
            reader.ReadToFollowing(); // at second
            reader.ReadToFollowing(); // at third-comp
            reader.ReadToFollowing(); // at inComp1
            Assert.AreEqual( reader.TagName, "inComp1" );
            Assert.AreEqual( reader.Skip(), 2 );
            Assert.AreEqual( reader.TagName, "fourth-list" );
            Assert.AreEqual( reader.Skip(), 10 );
            Assert.IsFalse( reader.ReadToFollowing() );
        }


        [Test]
        public void ReadAsTagTest() {
            // read the whole thing as tag
            {
                NbtFile file = new NbtFile( TestFiles.MakeValueTest() );
                using( MemoryStream ms = new MemoryStream( file.SaveToBuffer( NbtCompression.None ) ) ) {
                    NbtReader reader = new NbtReader( ms );
                    NbtCompound tag = (NbtCompound)reader.ReadAsTag();
                    TestFiles.AssertValueTest( new NbtFile( tag ) );
                }
            }

            // read various lists/compounds as tags
            {
                NbtReader reader = new NbtReader( TestFiles.MakeTest() );
                reader.ReadToFollowing(); // skip root
                while( !reader.IsAtStreamEnd ) {
                    reader.ReadAsTag();
                }
            }

            // read values as tags
            {
                byte[] testData = new NbtFile( TestFiles.MakeValueTest() ).SaveToBuffer( NbtCompression.None );
                NbtReader reader = new NbtReader( new MemoryStream( testData ) );
                reader.ReadToFollowing(); // skip root
                while( !reader.IsAtStreamEnd ) {
                    reader.ReadAsTag();
                }
            }

            // read a bunch of lists as tags
            {
                byte[] testData = new NbtFile( TestFiles.MakeListTest() ).SaveToBuffer( NbtCompression.None );
                NbtReader reader = new NbtReader( new MemoryStream( testData ) );
                reader.ReadToFollowing(); // skip root
                while( !reader.IsAtStreamEnd ) {
                    reader.ReadAsTag();
                }
            }
        }


        [Test]
        public void ReadListAsArray() {
            NbtCompound intList = TestFiles.MakeListTest();

            MemoryStream ms = new MemoryStream();
            new NbtFile( intList ).SaveToStream( ms, NbtCompression.None );
            ms.Seek( 0, SeekOrigin.Begin );
            NbtReader reader = new NbtReader( ms );

            // attempt to read value before we're in a list
            Assert.Throws<InvalidOperationException>( () => reader.ReadListAsArray<int>() );

            // test byte values
            reader.ReadToFollowing( "ByteList" );
            byte[] bytes = reader.ReadListAsArray<byte>();
            CollectionAssert.AreEqual( bytes,
                                       new byte[] {
                                           100, 20, 3
                                       } );

            // test double values
            reader.ReadToFollowing( "DoubleList" );
            double[] doubles = reader.ReadListAsArray<double>();
            CollectionAssert.AreEqual( doubles,
                                       new[] {
                                           1d, 2000d, -3000000d
                                       } );

            // test float values
            reader.ReadToFollowing( "FloatList" );
            float[] floats = reader.ReadListAsArray<float>();
            CollectionAssert.AreEqual( floats,
                                       new[] {
                                           1f, 2000f, -3000000f
                                       } );

            // test int values
            reader.ReadToFollowing( "IntList" );
            int[] ints = reader.ReadListAsArray<int>();
            CollectionAssert.AreEqual( ints,
                                       new[] {
                                           1, 2000, -3000000
                                       } );

            // test long values
            reader.ReadToFollowing( "LongList" );
            long[] longs = reader.ReadListAsArray<long>();
            CollectionAssert.AreEqual( longs,
                                       new[] {
                                           1L, 2000L, -3000000L
                                       } );

            // test short values
            reader.ReadToFollowing( "ShortList" );
            short[] shorts = reader.ReadListAsArray<short>();
            CollectionAssert.AreEqual( shorts,
                                       new short[] {
                                           1, 200, -30000
                                       } );

            // test short values
            reader.ReadToFollowing( "StringList" );
            string[] strings = reader.ReadListAsArray<string>();
            CollectionAssert.AreEqual( strings,
                                       new[] {
                                           "one", "two thousand", "negative three million"
                                       } );

            // try reading list of compounds (should fail)
            reader.ReadToFollowing( "CompoundList" );
            Assert.Throws<InvalidOperationException>( () => reader.ReadListAsArray<NbtCompound>() );

            // skip to the end of the stream
            while( reader.ReadToFollowing() ) {}
            Assert.Throws<EndOfStreamException>( () => reader.ReadListAsArray<int>() );
        }


        [Test]
        public void ReadListAsArrayRecast() {
            NbtCompound intList = TestFiles.MakeListTest();

            MemoryStream ms = new MemoryStream();
            new NbtFile( intList ).SaveToStream( ms, NbtCompression.None );
            ms.Seek( 0, SeekOrigin.Begin );
            NbtReader reader = new NbtReader( ms );

            // test bytes as shorts
            reader.ReadToFollowing( "ByteList" );
            short[] bytes = reader.ReadListAsArray<short>();
            CollectionAssert.AreEqual( bytes,
                                       new short[] {
                                           100, 20, 3
                                       } );
        }


        [Test]
        public void ReadValueTest() {
            byte[] testData = new NbtFile( TestFiles.MakeValueTest() ).SaveToBuffer( NbtCompression.None );
            NbtReader reader = new NbtReader( new MemoryStream( testData ) );

            Assert.IsTrue( reader.ReadToFollowing() ); // root

            Assert.IsTrue( reader.ReadToFollowing() ); // byte
            Assert.AreEqual( reader.ReadValue(), 1 );
            Assert.IsTrue( reader.ReadToFollowing() ); // short
            Assert.AreEqual( reader.ReadValue(), 2 );
            Assert.IsTrue( reader.ReadToFollowing() ); // int
            Assert.AreEqual( reader.ReadValue(), 3 );
            Assert.IsTrue( reader.ReadToFollowing() ); // long
            Assert.AreEqual( reader.ReadValue(), 4L );
            Assert.IsTrue( reader.ReadToFollowing() ); // float
            Assert.AreEqual( reader.ReadValue(), 5f );
            Assert.IsTrue( reader.ReadToFollowing() ); // double
            Assert.AreEqual( reader.ReadValue(), 6d );
            Assert.IsTrue( reader.ReadToFollowing() ); // byteArray
            CollectionAssert.AreEqual( (byte[])reader.ReadValue(), new byte[] { 10, 11, 12 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // intArray
            CollectionAssert.AreEqual( (int[])reader.ReadValue(), new[] { 20, 21, 22 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // string
            Assert.AreEqual( reader.ReadValue(), "123" );
        }


        [Test]
        public void ReadValueAsTest() {
            byte[] testData = new NbtFile( TestFiles.MakeValueTest() ).SaveToBuffer( NbtCompression.None );
            NbtReader reader = new NbtReader( new MemoryStream( testData ) );

            Assert.IsTrue( reader.ReadToFollowing() ); // root

            Assert.IsTrue( reader.ReadToFollowing() ); // byte
            Assert.AreEqual( reader.ReadValueAs<byte>(), 1 );
            Assert.IsTrue( reader.ReadToFollowing() ); // short
            Assert.AreEqual( reader.ReadValueAs<short>(), 2 );
            Assert.IsTrue( reader.ReadToFollowing() ); // int
            Assert.AreEqual( reader.ReadValueAs<int>(), 3 );
            Assert.IsTrue( reader.ReadToFollowing() ); // long
            Assert.AreEqual( reader.ReadValueAs<long>(), 4L );
            Assert.IsTrue( reader.ReadToFollowing() ); // float
            Assert.AreEqual( reader.ReadValueAs<float>(), 5f );
            Assert.IsTrue( reader.ReadToFollowing() ); // double
            Assert.AreEqual( reader.ReadValueAs<double>(), 6d );
            Assert.IsTrue( reader.ReadToFollowing() ); // byteArray
            CollectionAssert.AreEqual( reader.ReadValueAs<byte[]>(), new byte[] { 10, 11, 12 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // intArray
            CollectionAssert.AreEqual( reader.ReadValueAs<int[]>(), new[] { 20, 21, 22 } );
            Assert.IsTrue( reader.ReadToFollowing() ); // string
            Assert.AreEqual( reader.ReadValueAs<string>(), "123" );
        }


        [Test]
        public void ErrorTest() {
            NbtCompound root = new NbtCompound( "root" );
            byte[] testData = new NbtFile( root ).SaveToBuffer( NbtCompression.None );

            // corrupt the data
            testData[0] = 123;
            NbtReader reader = new NbtReader( new MemoryStream( testData ) );

            // attempt to use ReadValue when not at value
            Assert.Throws<InvalidOperationException>( () => reader.ReadValue() );
            reader.CacheTagValues = true;
            Assert.Throws<InvalidOperationException>( () => reader.ReadValue() );

            // attempt to read a corrupt stream
            Assert.Throws<NbtFormatException>( () => reader.ReadToFollowing() );

            // make sure we've properly entered the error state
            Assert.IsTrue( reader.IsInErrorState );
            Assert.IsFalse( reader.HasName );
            Assert.Throws<InvalidReaderStateException>( () => reader.ReadToFollowing() );
            Assert.Throws<InvalidReaderStateException>( () => reader.ReadListAsArray<int>() );
            Assert.Throws<InvalidReaderStateException>( () => reader.ReadToNextSibling() );
            Assert.Throws<InvalidReaderStateException>( () => reader.ReadToDescendant( "derp" ) );
            Assert.Throws<InvalidReaderStateException>( () => reader.ReadAsTag() );
            Assert.Throws<InvalidReaderStateException>( () => reader.Skip() );
        }


        [Test]
        public void NonSeekableStreamSkip() {
            byte[] fileBytes = File.ReadAllBytes( "TestFiles/bigtest.nbt" );
            using( MemoryStream ms = new MemoryStream( fileBytes ) ) {
                using( NonSeekableStream nss = new NonSeekableStream( ms ) ) {
                    NbtReader reader = new NbtReader( nss );
                    reader.ReadToFollowing();
                    reader.Skip();
                }
            }
        }
    }
}