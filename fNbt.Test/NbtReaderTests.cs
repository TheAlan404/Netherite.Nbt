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
                    new NbtInt( "inComp1" ),
                    new NbtInt( "inComp2" ),
                    new NbtInt( "inComp3" )
                },
                new NbtList( "fourth-list" ) {
                    new NbtList {
                        new NbtCompound {
                            new NbtCompound( "inList1" )
                        }
                    },
                    new NbtList {
                        new NbtCompound {
                            new NbtCompound( "inList2" )
                        }
                    },
                    new NbtList {
                        new NbtCompound {
                            new NbtCompound( "inList3" )
                        }
                    }
                },
                new NbtInt( "fifth" )
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
            Assert.AreEqual( reader.TagName, "fourth-list" );
            Assert.IsTrue( reader.ReadToNextSibling() );
            Assert.AreEqual( reader.TagName, "fifth" );
            Assert.IsFalse( reader.ReadToNextSibling() );
        }


        [Test]
        public void ReadToDescendantTest() {
            NbtReader reader = new NbtReader( MakeTest() );
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
            NbtReader reader = new NbtReader( MakeTest() );
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
            NbtReader reader = new NbtReader( MakeTest() );
            Debug.WriteLine( reader.ReadAsTag().ToString() );
        }
    }
}