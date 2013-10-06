using System;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public class NbtFileTests {
        const string TestDirName = "NbtFileTests";

        [SetUp]
        public void NbtFileTestSetup() {
            Directory.CreateDirectory( TestDirName );
        }


        #region Loading Small Nbt Test File

        [Test]
        public void TestNbtSmallFileLoadingUncompressed() {
            var file = new NbtFile( TestFiles.Small );
            Assert.AreEqual( file.FileName, TestFiles.Small );
            Assert.AreEqual( file.FileCompression, NbtCompression.None );
            AssertNbtSmallFile( file );
        }


        [Test]
        public void LoadingSmallFileGZip() {
            var file = new NbtFile( TestFiles.SmallGZip );
            Assert.AreEqual( file.FileCompression, NbtCompression.GZip );
            AssertNbtSmallFile( file );
        }


        [Test]
        public void LoadingSmallFileZLib() {
            var file = new NbtFile( TestFiles.SmallZLib );
            Assert.AreEqual( file.FileCompression, NbtCompression.ZLib );
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
        public void LoadingBigFileUncompressed() {
            var file = new NbtFile();
            int length = file.LoadFromFile( TestFiles.Big );
            AssertNbtBigFile( file );
            Assert.AreEqual( length, new FileInfo( TestFiles.Big ).Length );
        }


        [Test]
        public void LoadingBigFileGZip() {
            var file = new NbtFile();
            int length = file.LoadFromFile( TestFiles.BigGZip );
            AssertNbtBigFile( file );
            Assert.AreEqual( length, new FileInfo( TestFiles.BigGZip ).Length );
        }


        [Test]
        public void LoadingBigFileZLib() {
            var file = new NbtFile();
            int length = file.LoadFromFile( TestFiles.BigZLib );
            AssertNbtBigFile( file );
            Assert.AreEqual( length, new FileInfo( TestFiles.BigZLib ).Length );
        }


        [Test]
        public void LoadingBigFileBuffer() {
            byte[] fileBytes = File.ReadAllBytes( TestFiles.Big );
            var file = new NbtFile();
            int length = file.LoadFromBuffer( fileBytes, 0, fileBytes.Length, NbtCompression.AutoDetect, null );
            AssertNbtBigFile( file );
            Assert.AreEqual( length, new FileInfo( TestFiles.Big ).Length );
        }


        [Test]
        public void LoadingBigFileStream() {
            byte[] fileBytes = File.ReadAllBytes( TestFiles.Big );
            using( MemoryStream ms = new MemoryStream( fileBytes ) ) {
                using( NonSeekableStream nss = new NonSeekableStream( ms ) ) {
                    var file = new NbtFile();
                    int length = file.LoadFromStream( nss, NbtCompression.None, null );
                    AssertNbtBigFile( file );
                    Assert.AreEqual( length, new FileInfo( TestFiles.Big ).Length );
                }
            }
        }


        void AssertNbtBigFile( NbtFile file ) {
            // See TestFiles/bigtest.nbt.txt to see the expected format
            Assert.IsInstanceOf<NbtCompound>( file.RootTag );

            NbtCompound root = file.RootTag;
            Assert.AreEqual( "Level", root.Name );
            Assert.AreEqual( 12, root.Count );

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
            Assert.IsInstanceOf<NbtCompound>( node["ham"] );
            NbtCompound subNode = (NbtCompound)node["ham"];
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
            Assert.IsInstanceOf<NbtCompound>( node["egg"] );
            subNode = (NbtCompound)node["egg"];
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
                Assert.IsInstanceOf<NbtLong>( node[nodeIndex] );
                Assert.AreEqual( null, node[nodeIndex].Name );
                Assert.AreEqual( nodeIndex + 11, ( (NbtLong)node[nodeIndex] ).Value );
            }

            Assert.IsInstanceOf<NbtList>( root["listTest (compound)"] );
            node = root["listTest (compound)"];
            Assert.AreEqual( "listTest (compound)", node.Name );
            Assert.AreEqual( 2, ( (NbtList)node ).Count );

            // First Sub Node
            Assert.IsInstanceOf<NbtCompound>( node[0] );
            subNode = (NbtCompound)node[0];

            // First node in sub node
            Assert.IsInstanceOf<NbtString>( subNode["name"] );
            Assert.AreEqual( "name", subNode["name"].Name );
            Assert.AreEqual( "Compound tag #0", ( (NbtString)subNode["name"] ).Value );

            // Second node in sub node
            Assert.IsInstanceOf<NbtLong>( subNode["created-on"] );
            Assert.AreEqual( "created-on", subNode["created-on"].Name );
            Assert.AreEqual( 1264099775885, ( (NbtLong)subNode["created-on"] ).Value );

            // Second Sub Node
            Assert.IsInstanceOf<NbtCompound>( node[1] );
            subNode = (NbtCompound)node[1];

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

            const string byteArrayName =
                "byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))";
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

            Assert.IsInstanceOf<NbtIntArray>( root["intArrayTest"] );
            NbtIntArray intArrayTag = root.Get<NbtIntArray>( "intArrayTest" );
            Assert.IsNotNull( intArrayTag );
            Random rand = new Random( 0 );
            for( int i = 0; i < 10; i++ ) {
                Assert.AreEqual( intArrayTag.Value[i], rand.Next() );
            }
        }

        #endregion


        static NbtFile MakeSmallFile() {
            return new NbtFile(
                new NbtCompound( "hello world", new NbtTag[] {
                    new NbtString( "name", "Bananrama" )
                } )
            );
        }


        [Test]
        public void TestNbtSmallFileSavingUncompressed() {
            NbtFile file = MakeSmallFile();
            string testFileName = Path.Combine( TestDirName, "test.nbt" );
            file.SaveToFile( testFileName, NbtCompression.None );
            FileAssert.AreEqual( TestFiles.Small, testFileName );
        }


        [Test]
        public void TestNbtSmallFileSavingUncompressedStream() {
            NbtFile file = MakeSmallFile();
            MemoryStream nbtStream = new MemoryStream();
            file.SaveToStream( nbtStream, NbtCompression.None );
            FileStream testFileStream = File.OpenRead( TestFiles.Small );
            FileAssert.AreEqual( testFileStream, nbtStream );
        }


        [Test]
        public void ReloadFile() {
            ReloadFileInternal( "bigtest.nbt", NbtCompression.None, true );
            ReloadFileInternal( "bigtest.nbt.gz", NbtCompression.GZip, true );
            ReloadFileInternal( "bigtest.nbt.z", NbtCompression.ZLib, true );
            ReloadFileInternal( "bigtest.nbt", NbtCompression.None, false );
            ReloadFileInternal( "bigtest.nbt.gz", NbtCompression.GZip, false );
            ReloadFileInternal( "bigtest.nbt.z", NbtCompression.ZLib, false );
        }


        void ReloadFileInternal( String fileName, NbtCompression compression, bool bigEndian ) {
            NbtFile loadedFile = new NbtFile( Path.Combine( TestFiles.DirName, fileName ) );
            loadedFile.BigEndian = bigEndian;
            int bytesWritten = loadedFile.SaveToFile( Path.Combine( TestDirName, fileName ), compression );
            int bytesRead = loadedFile.LoadFromFile( Path.Combine(TestDirName,fileName), NbtCompression.AutoDetect, null );
            Assert.AreEqual( bytesWritten, bytesRead );
            AssertNbtBigFile( loadedFile );
        }


        [Test]
        public void ReloadNonSeekableStream() {
            NbtFile loadedFile = new NbtFile(TestFiles.Big );
            using( MemoryStream ms = new MemoryStream() ) {
                using( NonSeekableStream nss = new NonSeekableStream( ms ) ) {
                    int bytesWritten = loadedFile.SaveToStream( nss, NbtCompression.None );
                    ms.Position = 0;
                    int bytesRead = loadedFile.LoadFromStream( nss, NbtCompression.None, null );
                    Assert.AreEqual( bytesWritten, bytesRead );
                    AssertNbtBigFile( loadedFile );
                }
            }
        }


        [Test]
        public void LoadFromStream() {
            Assert.Throws<ArgumentNullException>( () => new NbtFile().LoadFromStream( null, NbtCompression.AutoDetect ) );

            LoadFromStreamInternal( TestFiles.Big, NbtCompression.None );
            LoadFromStreamInternal( TestFiles.BigGZip, NbtCompression.GZip );
            LoadFromStreamInternal( TestFiles.BigZLib, NbtCompression.ZLib );
        }


        void LoadFromStreamInternal( String fileName, NbtCompression compression ) {
            NbtFile file = new NbtFile();
            byte[] fileBytes = File.ReadAllBytes( fileName );
            using( MemoryStream ms = new MemoryStream( fileBytes ) ) {
                file.LoadFromStream( ms, compression );
            }
        }


        [Test]
        public void PrettyPrint() {
            NbtFile loadedFile = new NbtFile( TestFiles.Big );
            Assert.AreEqual( loadedFile.ToString(), loadedFile.RootTag.ToString() );
            Assert.AreEqual( loadedFile.ToString( "   " ), loadedFile.RootTag.ToString( "   " ) );
            Assert.Throws<ArgumentNullException>( () => loadedFile.ToString( null ) );
            Assert.Throws<ArgumentNullException>( () => NbtTag.DefaultIndentString = null );
        }


        [Test]
        public void ReadRootTag() {
            Assert.Throws<ArgumentNullException>( () => NbtFile.ReadRootTagName( null ) );
            Assert.Throws<FileNotFoundException>( () => NbtFile.ReadRootTagName( "NonExistentFile" ) );
            Assert.Throws<ArgumentNullException>(
                () => NbtFile.ReadRootTagName( (Stream)null, NbtCompression.None, true, 0 ) );

            ReadRootTagInternal( TestFiles.Big, NbtCompression.None );
            ReadRootTagInternal( TestFiles.BigGZip, NbtCompression.GZip );
            ReadRootTagInternal( TestFiles.BigZLib, NbtCompression.ZLib );
        }


        void ReadRootTagInternal( String fileName, NbtCompression compression ) {
            Assert.Throws<ArgumentOutOfRangeException>( () => NbtFile.ReadRootTagName( fileName, compression, true, -1 ) );

            Assert.AreEqual( NbtFile.ReadRootTagName( fileName ), "Level" );
            Assert.AreEqual( NbtFile.ReadRootTagName( fileName, compression, true, 0 ), "Level" );

            byte[] fileBytes = File.ReadAllBytes( fileName );
            using( MemoryStream ms = new MemoryStream( fileBytes ) ) {
                using( NonSeekableStream nss = new NonSeekableStream( ms ) ) {
                    Assert.Throws<ArgumentOutOfRangeException>(
                        () => NbtFile.ReadRootTagName( nss, compression, true, -1 ) );
                    NbtFile.ReadRootTagName( nss, compression, true, 0 );
                }
            }
        }


        [TearDown]
        public void NbtFileTestTearDown() {
            if( Directory.Exists( TestDirName ) ) {
                foreach( var file in Directory.GetFiles( TestDirName ) ) {
                    File.Delete( file );
                }
                Directory.Delete( TestDirName );
            }
        }
    }
}