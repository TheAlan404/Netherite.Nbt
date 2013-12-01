using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class TagSelectorTests {
        [Test]
        public void SkippingTagsOnFileLoad() {
            var loadedFile = new NbtFile();
            loadedFile.LoadFromFile( "TestFiles/bigtest.nbt",
                                     NbtCompression.None,
                                     tag => tag.Name != "nested compound test" );
            Assert.IsFalse( loadedFile.RootTag.Contains( "nested compound test" ) );
            Assert.IsTrue( loadedFile.RootTag.Contains( "listTest (long)" ) );

            loadedFile.LoadFromFile( "TestFiles/bigtest.nbt",
                                     NbtCompression.None,
                                     tag => tag.TagType != NbtTagType.Float || tag.Parent.Name != "Level" );
            Assert.IsFalse( loadedFile.RootTag.Contains( "floatTest" ) );
            Assert.AreEqual( loadedFile.RootTag["nested compound test"]["ham"]["value"].FloatValue, 0.75 );

            loadedFile.LoadFromFile( "TestFiles/bigtest.nbt",
                                     NbtCompression.None,
                                     tag => tag.Name != "listTest (long)" );
            Assert.IsFalse( loadedFile.RootTag.Contains( "listTest (long)" ) );
            Assert.IsTrue( loadedFile.RootTag.Contains( "byteTest" ) );

            loadedFile.LoadFromFile( "TestFiles/bigtest.nbt",
                                     NbtCompression.None,
                                     tag => false );
            Assert.AreEqual( loadedFile.RootTag.Count, 0 );
        }


        [Test]
        public void SkippingLists() {
            var file = new NbtFile( TestFiles.MakeListTest() );
            byte[] savedFile = file.SaveToBuffer( NbtCompression.None );
            file.LoadFromBuffer( savedFile, 0, savedFile.Length, NbtCompression.None, tag => false );
            Assert.AreEqual( file.RootTag.Count, 0 );
        }


        [Test]
        public void SkippingValuesInCompoundTest() {
            NbtCompound root = TestFiles.MakeValueTest();
            NbtCompound nestedComp = TestFiles.MakeValueTest();
            nestedComp.Name = "NestedComp";
            root.Add( nestedComp );

            var file = new NbtFile( root );
            byte[] savedFile = file.SaveToBuffer( NbtCompression.None );
            file.LoadFromBuffer( savedFile, 0, savedFile.Length, NbtCompression.None, tag => false );
            Assert.AreEqual( file.RootTag.Count, 0 );
        }
    }
}