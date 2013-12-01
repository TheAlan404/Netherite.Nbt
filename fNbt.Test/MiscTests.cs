using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public class MiscTests {
        [Test]
        public void ByteArrayIndexerTest() {
            // test getting/settings values of byte array tag via indexer
            NbtByteArray byteArray = new NbtByteArray( "Test" );
            CollectionAssert.AreEqual( byteArray.Value, new byte[0] );
            byteArray.Value = new byte[] {
                1, 2, 3
            };
            Assert.AreEqual( byteArray[0], 1 );
            Assert.AreEqual( byteArray[1], 2 );
            Assert.AreEqual( byteArray[2], 3 );
            byteArray[0] = 4;
            Assert.AreEqual( byteArray[0], 4 );
        }


        [Test]
        public void IntArrayIndexerTest() {
            // test getting/settings values of int array tag via indexer
            NbtIntArray byteArray = new NbtIntArray( "Test" );
            CollectionAssert.AreEqual( byteArray.Value, new int[0] );
            byteArray.Value = new int[] {
                1, 2000, -3000000
            };
            Assert.AreEqual( byteArray[0], 1 );
            Assert.AreEqual( byteArray[1], 2000 );
            Assert.AreEqual( byteArray[2], -3000000 );
            byteArray[0] = 4;
            Assert.AreEqual( byteArray[0], 4 );
        }


        [Test]
        public void DefaultValueTest() {
            // test default values of all value tags
            Assert.AreEqual( new NbtByte( "test" ).Value, 0 );
            CollectionAssert.AreEqual( new NbtByteArray( "test" ).Value, new byte[0] );
            Assert.AreEqual( new NbtDouble( "test" ).Value, 0d );
            Assert.AreEqual( new NbtFloat( "test" ).Value, 0f );
            Assert.AreEqual( new NbtInt( "test" ).Value, 0 );
            CollectionAssert.AreEqual( new NbtIntArray( "test" ).Value, new int[0] );
            Assert.AreEqual( new NbtLong( "test" ).Value, 0L );
            Assert.AreEqual( new NbtShort( "test" ).Value, 0 );
            Assert.AreEqual( new NbtString().Value, "" );
        }


        [Test]
        public void PathTest() {
            // test NbtTag.Path property
            NbtCompound testComp = new NbtCompound {
                new NbtCompound( "Compound" ) {
                    new NbtCompound( "InsideCompound" )
                },
                new NbtList( "List" ) {
                    new NbtCompound {
                        new NbtInt( "InsideCompoundAndList" )
                    }
                }
            };

            // parent-less tag with no name has empty string for a path
            Assert.AreEqual( testComp.Path, "" );
            Assert.AreEqual( testComp["Compound"].Path, ".Compound" );
            Assert.AreEqual( testComp["Compound"]["InsideCompound"].Path, ".Compound.InsideCompound" );
            Assert.AreEqual( testComp["List"].Path, ".List" );

            // tags inside lists have no name, but they do have an index
            Assert.AreEqual( testComp["List"][0].Path, ".List[0]" );
            Assert.AreEqual( testComp["List"][0]["InsideCompoundAndList"].Path, ".List[0].InsideCompoundAndList" );
        }
    }
}
