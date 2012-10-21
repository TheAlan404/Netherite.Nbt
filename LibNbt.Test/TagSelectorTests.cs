using System;
using System.IO;
using NUnit.Framework;

namespace LibNbt.Test {
    [TestFixture]
    public sealed class TagSelectorTests {
        [Test]
        public void InitializingListFromCollection() {
            NbtFile loadedFile = new NbtFile( "TestFiles/bigtest.nbt",
                                              NbtCompression.None,
                                              tag => tag.Name != "nested compound test" );
            Assert.IsFalse( loadedFile.RootTag.Contains( "nested compound test" ) );
            Assert.IsTrue( loadedFile.RootTag.Contains( "listTest (long)" ) );

            loadedFile.LoadFromFile( "TestFiles/bigtest.nbt",
                                     NbtCompression.None,
                                     tag => tag.TagType != NbtTagType.Float || tag.Parent.Name != "Level" );
            Assert.IsFalse( loadedFile.RootTag.Contains( "floatTest" ) );
            Assert.AreEqual( loadedFile.RootTag["nested compound test"]["ham"]["value"].FloatValue, 0.75 );
        }
    }
}