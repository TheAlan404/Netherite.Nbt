using System;
using LibNbt.Tags;
using NUnit.Framework;

namespace LibNbt.Test.Tags {
    [TestFixture]
    public class ListTests {
        [Test]
        public void InitializingListFromCollection() {
            var sameTags = new NbtTag[] {
                new NbtInt( 1 ),
                new NbtInt( 2 ),
                new NbtInt( 3 )
            };
            var mixedTags = new NbtTag[] {
                new NbtFloat( 1 ),
                new NbtByte( 2 ),
                new NbtInt( 3 )
            };

            // auto-detecting list type
            Assert.DoesNotThrow( () => new NbtList( "Test1", sameTags ) );
            Assert.AreEqual( new NbtList( "Test1", sameTags ).ListType, NbtTagType.Int );

            // correct explicitly-given list type
            Assert.DoesNotThrow( () => new NbtList( "Test2", sameTags, NbtTagType.Int ) );

            // wrong explicitly-given list type
            Assert.Throws<ArgumentException>( () => new NbtList( "Test3", sameTags, NbtTagType.Float ) );

            // auto-detecting mixed list given
            Assert.Throws<ArgumentException>( () => new NbtList( "Test4", mixedTags ) );
        }


        [Test]
        public void ChangingValidListTagType() {
            var list = new NbtList();

            // changing type of an empty list
            Assert.DoesNotThrow( () => list.ListType = NbtTagType.Unknown );

            list.Add( new NbtInt() );

            // setting correct type for a non-empty list
            Assert.DoesNotThrow( () => list.ListType = NbtTagType.Int );

            // changing list type to an incorrect type
            Assert.Throws<Exception>( () => list.ListType = NbtTagType.Short );
        }
    }
}