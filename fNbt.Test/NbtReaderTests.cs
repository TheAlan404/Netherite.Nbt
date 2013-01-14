using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class NbtReaderTests {
        [Test]
        public void PrintBigFileUncompressed() {
            using( FileStream fs = File.OpenRead( "TestFiles/bigtest.nbt" ) ) {
                NbtReader reader = new NbtReader( fs );
                while( reader.ReadToFollowing() ) {
                    Debug.WriteLine( reader.ToStringWithValue() );
                }
            }
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
                    Debug.WriteLine( reader.ToStringWithValue() );
                }
            }
        }


        static Stream MakeTest() {
            NbtCompound root = new NbtCompound( "root" ) {
                new NbtInt( "first" ),
                new NbtInt( "second" ),
                new NbtCompound( "third-comp" ) {
                    new NbtInt( "sub1" ),
                    new NbtInt( "sub2" ),
                    new NbtInt( "sub3" )
                },
                new NbtInt( "fourth" )
            };
            byte[] testData = new NbtFile( root ).SaveToBuffer( NbtCompression.None );
            return new MemoryStream( testData );
        }


        [Test]
        public void ReadToSiblingTest() {
            NbtReader reader = new NbtReader( MakeTest() );
            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "root" );
            Assert.IsTrue( reader.ReadToFollowing() );
            Assert.AreEqual( reader.TagName, "first" );
            Assert.IsTrue( reader.ReadToNextSibling( "third-comp" ) );
            Assert.AreEqual( reader.TagName, "third-comp" );
            Assert.IsTrue( reader.ReadToNextSibling() );
            Assert.AreEqual( reader.TagName, "fourth" );
            Assert.IsFalse( reader.ReadToNextSibling() );
        }


        [Test]
        public void ReadToDescendantTest() {
            {
                NbtReader reader = new NbtReader( MakeTest() );
                Assert.IsTrue( reader.ReadToDescendant( "third-comp" ) );
                Assert.AreEqual( reader.TagName, "third-comp" );
                Assert.IsTrue( reader.ReadToDescendant( "sub3" ) );
                Assert.AreEqual( reader.TagName, "sub3" );
                Assert.IsFalse( reader.ReadToDescendant( "derp" ) );
                Assert.AreEqual( reader.TagName, "fourth" );
            }
            {
                NbtReader reader = new NbtReader( MakeTest() );
                Assert.IsTrue( reader.ReadToDescendant( "sub2" ) );
                Assert.AreEqual( reader.TagName, "sub2" );
            }
        }


        [Test]
        public void SkipTest() {
            NbtReader reader = new NbtReader( MakeTest() );
            reader.ReadToFollowing(); // at root
            reader.ReadToFollowing(); // at first
            Assert.AreEqual( reader.TagName, "first" );
            Assert.AreEqual( reader.Skip(), 6 );
            Assert.IsFalse( reader.ReadToFollowing() );
        }
    }
}