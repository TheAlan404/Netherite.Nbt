using System;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public class NbtFileTests {
        const string TestDirName = "NbtFileTests";


        [SetUp]
        public void NbtFileTestSetup() {
            Directory.CreateDirectory(TestDirName);
        }


        #region Loading Small Nbt Test File

        [Test]
        public void TestNbtSmallFileLoadingUncompressed() {
            var file = new NbtFile(TestFiles.Small);
            Assert.AreEqual(file.FileName, TestFiles.Small);
            Assert.AreEqual(file.FileCompression, NbtCompression.None);
            TestFiles.AssertNbtSmallFile(file);
        }


        [Test]
        public void LoadingSmallFileGZip() {
            var file = new NbtFile(TestFiles.SmallGZip);
            Assert.AreEqual(file.FileCompression, NbtCompression.GZip);
            TestFiles.AssertNbtSmallFile(file);
        }


        [Test]
        public void LoadingSmallFileZLib() {
            var file = new NbtFile(TestFiles.SmallZLib);
            Assert.AreEqual(file.FileCompression, NbtCompression.ZLib);
            TestFiles.AssertNbtSmallFile(file);
        }

        #endregion


        #region Loading Big Nbt Test File

        [Test]
        public void LoadingBigFileUncompressed() {
            var file = new NbtFile();
            int length = file.LoadFromFile(TestFiles.Big);
            TestFiles.AssertNbtBigFile(file);
            Assert.AreEqual(length, new FileInfo(TestFiles.Big).Length);
        }


        [Test]
        public void LoadingBigFileGZip() {
            var file = new NbtFile();
            int length = file.LoadFromFile(TestFiles.BigGZip);
            TestFiles.AssertNbtBigFile(file);
            Assert.AreEqual(length, new FileInfo(TestFiles.BigGZip).Length);
        }


        [Test]
        public void LoadingBigFileZLib() {
            var file = new NbtFile();
            int length = file.LoadFromFile(TestFiles.BigZLib);
            TestFiles.AssertNbtBigFile(file);
            Assert.AreEqual(length, new FileInfo(TestFiles.BigZLib).Length);
        }


        [Test]
        public void LoadingBigFileBuffer() {
            byte[] fileBytes = File.ReadAllBytes(TestFiles.Big);
            var file = new NbtFile();
            int length = file.LoadFromBuffer(fileBytes, 0, fileBytes.Length, NbtCompression.AutoDetect, null);
            TestFiles.AssertNbtBigFile(file);
            Assert.AreEqual(length, new FileInfo(TestFiles.Big).Length);
        }


        [Test]
        public void LoadingBigFileStream() {
            byte[] fileBytes = File.ReadAllBytes(TestFiles.Big);
            using (var ms = new MemoryStream(fileBytes)) {
                using (var nss = new NonSeekableStream(ms)) {
                    var file = new NbtFile();
                    int length = file.LoadFromStream(nss, NbtCompression.None, null);
                    TestFiles.AssertNbtBigFile(file);
                    Assert.AreEqual(length, new FileInfo(TestFiles.Big).Length);
                }
            }
        }

        #endregion


        [Test]
        public void TestNbtSmallFileSavingUncompressed() {
            NbtFile file = TestFiles.MakeSmallFile();
            string testFileName = Path.Combine(TestDirName, "test.nbt");
            file.SaveToFile(testFileName, NbtCompression.None);
            FileAssert.AreEqual(TestFiles.Small, testFileName);
        }


        [Test]
        public void TestNbtSmallFileSavingUncompressedStream() {
            NbtFile file = TestFiles.MakeSmallFile();
            var nbtStream = new MemoryStream();
            file.SaveToStream(nbtStream, NbtCompression.None);
            FileStream testFileStream = File.OpenRead(TestFiles.Small);
            FileAssert.AreEqual(testFileStream, nbtStream);
        }


        [Test]
        public void ReloadFile() {
            ReloadFileInternal("bigtest.nbt", NbtCompression.None, true);
            ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, true);
            ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, true);
            ReloadFileInternal("bigtest.nbt", NbtCompression.None, false);
            ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, false);
            ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, false);
        }


        void ReloadFileInternal(String fileName, NbtCompression compression, bool bigEndian) {
            var loadedFile = new NbtFile(Path.Combine(TestFiles.DirName, fileName));
            loadedFile.BigEndian = bigEndian;
            int bytesWritten = loadedFile.SaveToFile(Path.Combine(TestDirName, fileName), compression);
            int bytesRead = loadedFile.LoadFromFile(Path.Combine(TestDirName, fileName), NbtCompression.AutoDetect, null);
            Assert.AreEqual(bytesWritten, bytesRead);
            TestFiles.AssertNbtBigFile(loadedFile);
        }


        [Test]
        public void ReloadNonSeekableStream() {
            var loadedFile = new NbtFile(TestFiles.Big);
            using (var ms = new MemoryStream()) {
                using (var nss = new NonSeekableStream(ms)) {
                    int bytesWritten = loadedFile.SaveToStream(nss, NbtCompression.None);
                    ms.Position = 0;
                    Assert.Throws<NotSupportedException>(() => loadedFile.LoadFromStream(nss, NbtCompression.AutoDetect));
                    ms.Position = 0;
                    Assert.Throws<InvalidDataException>(() => loadedFile.LoadFromStream(nss, NbtCompression.ZLib));
                    ms.Position = 0;
                    int bytesRead = loadedFile.LoadFromStream(nss, NbtCompression.None);
                    Assert.AreEqual(bytesWritten, bytesRead);
                    TestFiles.AssertNbtBigFile(loadedFile);
                }
            }
        }


        [Test]
        public void LoadFromStream() {
            Assert.Throws<ArgumentNullException>(() => new NbtFile().LoadFromStream(null, NbtCompression.AutoDetect));

            LoadFromStreamInternal(TestFiles.Big, NbtCompression.None);
            LoadFromStreamInternal(TestFiles.BigGZip, NbtCompression.GZip);
            LoadFromStreamInternal(TestFiles.BigZLib, NbtCompression.ZLib);
        }


        void LoadFromStreamInternal(String fileName, NbtCompression compression) {
            var file = new NbtFile();
            byte[] fileBytes = File.ReadAllBytes(fileName);
            using (var ms = new MemoryStream(fileBytes)) {
                file.LoadFromStream(ms, compression);
            }
        }


        [Test]
        public void SaveToBuffer() {
            var littleTag = new NbtCompound("Root");
            var testFile = new NbtFile(littleTag);

            byte[] buffer1 = testFile.SaveToBuffer(NbtCompression.None);
            var buffer2 = new byte[buffer1.Length];
            Assert.AreEqual(testFile.SaveToBuffer(buffer2, 0, NbtCompression.None), buffer2.Length);
            CollectionAssert.AreEqual(buffer1, buffer2);
        }


        [Test]
        public void PrettyPrint() {
            var loadedFile = new NbtFile(TestFiles.Big);
            Assert.AreEqual(loadedFile.ToString(), loadedFile.RootTag.ToString());
            Assert.AreEqual(loadedFile.ToString("   "), loadedFile.RootTag.ToString("   "));
            Assert.Throws<ArgumentNullException>(() => loadedFile.ToString(null));
            Assert.Throws<ArgumentNullException>(() => NbtTag.DefaultIndentString = null);
        }


        [Test]
        public void ReadRootTag() {
            Assert.Throws<ArgumentNullException>(() => NbtFile.ReadRootTagName(null));
            Assert.Throws<FileNotFoundException>(() => NbtFile.ReadRootTagName("NonExistentFile"));
            Assert.Throws<ArgumentNullException>(
                () => NbtFile.ReadRootTagName((Stream)null, NbtCompression.None, true, 0));

            ReadRootTagInternal(TestFiles.Big, NbtCompression.None);
            ReadRootTagInternal(TestFiles.BigGZip, NbtCompression.GZip);
            ReadRootTagInternal(TestFiles.BigZLib, NbtCompression.ZLib);
        }


        void ReadRootTagInternal(String fileName, NbtCompression compression) {
            Assert.Throws<ArgumentOutOfRangeException>(() => NbtFile.ReadRootTagName(fileName, compression, true, -1));

            Assert.AreEqual(NbtFile.ReadRootTagName(fileName), "Level");
            Assert.AreEqual(NbtFile.ReadRootTagName(fileName, compression, true, 0), "Level");

            byte[] fileBytes = File.ReadAllBytes(fileName);
            using (var ms = new MemoryStream(fileBytes)) {
                using (var nss = new NonSeekableStream(ms)) {
                    Assert.Throws<ArgumentOutOfRangeException>(
                        () => NbtFile.ReadRootTagName(nss, compression, true, -1));
                    NbtFile.ReadRootTagName(nss, compression, true, 0);
                }
            }
        }


        [TearDown]
        public void NbtFileTestTearDown() {
            if (Directory.Exists(TestDirName)) {
                foreach (string file in Directory.GetFiles(TestDirName)) {
                    File.Delete(file);
                }
                Directory.Delete(TestDirName);
            }
        }
    }
}
