using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class NbtReaderTests {
        [Test]
        public void ReadBigFileUncompressed() {
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
    }
}