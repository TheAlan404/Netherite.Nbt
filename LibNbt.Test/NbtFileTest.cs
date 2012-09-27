using System;
using System.IO;
using LibNbt.Tags;
using NUnit.Framework;

namespace LibNbt.Test {
    [TestFixture]
    public class NbtFileTest {
        [SetUp]
        public void NbtFileTestSetup() {
            Directory.CreateDirectory( "TestTemp" );
        }


        [TearDown]
        public void NbtFileTestTearDown() {
            if( Directory.Exists( "TestTemp" ) ) {
                foreach( var file in Directory.GetFiles( "TestTemp" ) ) {
                    File.Delete( file );
                }
                Directory.Delete( "TestTemp" );
            }
        }


        #region Loading Small Nbt Test File

        [Test]
        public void TestNbtSmallFileLoading() {
            var file = new NbtFile();
            file.LoadFile( "TestFiles/test.nbt.gz" );

            AssertNbtSmallFile( file );
        }


        [Test]
        public void TestNbtSmallFileLoadingUncompressed() {
            var file = new NbtFile();
            file.LoadFile( "TestFiles/test.nbt", NbtCompression.None );

            AssertNbtSmallFile( file );
        }


        void AssertNbtSmallFile( NbtFile file ) {
            // See TestFiles/test.nbt.txt to see the expected format
            Assert.IsInstanceOf<NbtCompound>( file.RootTag );

            NbtCompound root = file.RootTag;
            Assert.AreEqual( "hello world", root.Name );
            Assert.AreEqual( 1, root.Count );

            Assert.IsInstanceOf<NbtString>( root["name"] );

            var node = (NbtString)root["name"];
            Assert.AreEqual( "name", node.Name );
            Assert.AreEqual( "Bananrama", node.Value );
        }

        #endregion


        #region Loading Big Nbt Test File

        [Test]
        public void TestNbtBigFileLoading() {
            var file = new NbtFile();
            file.LoadFile( "TestFiles/bigtest.nbt.gz" );

            AssertNbtBigFile( file );
        }


        [Test]
        public void TestnbtBigFileLoadingUncompressed() {
            var file = new NbtFile();
            file.LoadFile( "TestFiles/bigtest.nbt", NbtCompression.None );

            AssertNbtBigFile( file );
        }


        void AssertNbtBigFile( NbtFile file ) {
            // See TestFiles/bigtest.nbt.txt to see the expected format
            Assert.IsInstanceOf<NbtCompound>( file.RootTag );

            NbtCompound root = file.RootTag;
            Assert.AreEqual( "Level", root.Name );
            Assert.AreEqual( 11, root.Count );

            Assert.IsInstanceOf<NbtLong>( root["longTest"] );
            NbtTag node = root["longTest"];
            Assert.AreEqual( "longTest", node.Name );
            Assert.AreEqual( 9223372036854775807, ( (NbtLong)node ).Value );

            Assert.IsInstanceOf<NbtShort>( root["shortTest"] );
            node = root["shortTest"];
            Assert.AreEqual( "shortTest", node.Name );
            Assert.AreEqual( 32767, ( (NbtShort)node ).Value );

            Assert.IsInstanceOf<NbtString>( root["stringTest"] );
            node = root["stringTest"];
            Assert.AreEqual( "stringTest", node.Name );
            Assert.AreEqual( "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!", ( (NbtString)node ).Value );

            Assert.IsInstanceOf<NbtFloat>( root["floatTest"] );
            node = root["floatTest"];
            Assert.AreEqual( "floatTest", node.Name );
            Assert.AreEqual( 0.49823147f, ( (NbtFloat)node ).Value );

            Assert.IsInstanceOf<NbtInt>( root["intTest"] );
            node = root["intTest"];
            Assert.AreEqual( "intTest", node.Name );
            Assert.AreEqual( 2147483647, ( (NbtInt)node ).Value );

            Assert.IsInstanceOf<NbtCompound>( root["nested compound test"] );
            node = root["nested compound test"];
            Assert.AreEqual( "nested compound test", node.Name );
            Assert.AreEqual( 2, ( (NbtCompound)node ).Count );

            // First nested test
            Assert.IsInstanceOf<NbtCompound>( ( (NbtCompound)node )["ham"] );
            NbtCompound subNode = (NbtCompound)( (NbtCompound)node )["ham"];
            Assert.AreEqual( "ham", subNode.Name );
            Assert.AreEqual( 2, subNode.Count );

            // Checking sub node values
            Assert.IsInstanceOf<NbtString>( subNode["name"] );
            Assert.AreEqual( "name", subNode["name"].Name );
            Assert.AreEqual( "Hampus", ( (NbtString)subNode["name"] ).Value );

            Assert.IsInstanceOf<NbtFloat>( subNode["value"] );
            Assert.AreEqual( "value", subNode["value"].Name );
            Assert.AreEqual( 0.75, ( (NbtFloat)subNode["value"] ).Value );
            // End sub node

            // Second nested test
            Assert.IsInstanceOf<NbtCompound>( ( (NbtCompound)node )["egg"] );
            subNode = (NbtCompound)( (NbtCompound)node )["egg"];
            Assert.AreEqual( "egg", subNode.Name );
            Assert.AreEqual( 2, subNode.Count );

            // Checking sub node values
            Assert.IsInstanceOf<NbtString>( subNode["name"] );
            Assert.AreEqual( "name", subNode["name"].Name );
            Assert.AreEqual( "Eggbert", ( (NbtString)subNode["name"] ).Value );

            Assert.IsInstanceOf<NbtFloat>( subNode["value"] );
            Assert.AreEqual( "value", subNode["value"].Name );
            Assert.AreEqual( 0.5, ( (NbtFloat)subNode["value"] ).Value );
            // End sub node

            Assert.IsInstanceOf<NbtList>( root["listTest (long)"] );
            node = root["listTest (long)"];
            Assert.AreEqual( "listTest (long)", node.Name );
            Assert.AreEqual( 5, ( (NbtList)node ).Count );

            // The values should be: 11, 12, 13, 14, 15
            for( int nodeIndex = 0; nodeIndex < ( (NbtList)node ).Count; nodeIndex++ ) {
                Assert.IsInstanceOf<NbtLong>( ( (NbtList)node )[nodeIndex] );
                Assert.AreEqual( null, ( (NbtList)node )[nodeIndex].Name );
                Assert.AreEqual( nodeIndex + 11, ( (NbtLong)( (NbtList)node )[nodeIndex] ).Value );
            }

            Assert.IsInstanceOf<NbtList>( root["listTest (compound)"] );
            node = root["listTest (compound)"];
            Assert.AreEqual( "listTest (compound)", node.Name );
            Assert.AreEqual( 2, ( (NbtList)node ).Count );

            // First Sub Node
            Assert.IsInstanceOf<NbtCompound>( ( (NbtList)node )[0] );
            subNode = (NbtCompound)( (NbtList)node )[0];

            // First node in sub node
            Assert.IsInstanceOf<NbtString>( subNode["name"] );
            Assert.AreEqual( "name", subNode["name"].Name );
            Assert.AreEqual( "Compound tag #0", ( (NbtString)subNode["name"] ).Value );

            // Second node in sub node
            Assert.IsInstanceOf<NbtLong>( subNode["created-on"] );
            Assert.AreEqual( "created-on", subNode["created-on"].Name );
            Assert.AreEqual( 1264099775885, ( (NbtLong)subNode["created-on"] ).Value );

            // Second Sub Node
            Assert.IsInstanceOf<NbtCompound>( ( (NbtList)node )[1] );
            subNode = (NbtCompound)( (NbtList)node )[1];

            // First node in sub node
            Assert.IsInstanceOf<NbtString>( subNode["name"] );
            Assert.AreEqual( "name", subNode["name"].Name );
            Assert.AreEqual( "Compound tag #1", ( (NbtString)subNode["name"] ).Value );

            // Second node in sub node
            Assert.IsInstanceOf<NbtLong>( subNode["created-on"] );
            Assert.AreEqual( "created-on", subNode["created-on"].Name );
            Assert.AreEqual( 1264099775885, ( (NbtLong)subNode["created-on"] ).Value );

            Assert.IsInstanceOf<NbtByte>( root["byteTest"] );
            node = root["byteTest"];
            Assert.AreEqual( "byteTest", node.Name );
            Assert.AreEqual( 127, ( (NbtByte)node ).Value );

            const string byteArrayName = "byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))";
            Assert.IsInstanceOf<NbtByteArray>( root[byteArrayName] );
            node = root[byteArrayName];
            Assert.AreEqual( byteArrayName, node.Name );
            Assert.AreEqual( 1000, ( (NbtByteArray)node ).Value.Length );

            // Values are: the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...)
            for( int n = 0; n < 1000; n++ ) {
                Assert.AreEqual( ( n * n * 255 + n * 7 ) % 100, ( (NbtByteArray)node )[n] );
            }

            Assert.IsInstanceOf<NbtDouble>( root["doubleTest"] );
            node = root["doubleTest"];
            Assert.AreEqual( "doubleTest", node.Name );
            Assert.AreEqual( 0.4931287132182315, ( (NbtDouble)node ).Value );
        }

        #endregion


        [Test]
        public void TestNbtSmallFileSavingUncompressed() {
            var file = new NbtFile {
                RootTag = new NbtCompound( "hello world", new NbtTag[] {
                    new NbtString( "name", "Bananrama" )
                } )
            };

            file.SaveFile( "TestTemp/test.nbt", NbtCompression.None );

            FileAssert.AreEqual( "TestFiles/test.nbt", "TestTemp/test.nbt" );
        }


        [Test]
        public void TestNbtSmallFileSavingUncompressedStream() {
            var file = new NbtFile {
                RootTag = new NbtCompound( "hello world", new NbtTag[] {
                    new NbtString( "name", "Bananrama" )
                } )
            };

            var nbtStream = new MemoryStream();
            file.SaveFile( nbtStream, NbtCompression.None );

            FileStream testFileStream = File.OpenRead( "TestFiles/test.nbt" );

            FileAssert.AreEqual( testFileStream, nbtStream );
        }
    }
}