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
        public void ManipulatingList() {
            var sameTags = new NbtTag[] {
                new NbtInt( 0 ),
                new NbtInt( 1 ),
                new NbtInt( 2 )
            };

            NbtList list = new NbtList( "Test1", sameTags );

            // adding an item of correct type
            Assert.DoesNotThrow( () => list.Add( new NbtInt( 3 ) ) );

            // adding an item of wrong type
            Assert.Throws<ArgumentException>( () => list.Add( new NbtString() ) );

            // testing array contents
            for( int i = 0; i < sameTags.Length; i++ ) {
                Assert.AreSame( sameTags[i], list[i] );
                Assert.AreEqual( ( (NbtInt)list[i] ).Value, i );
            }

            // test removal
            Assert.IsFalse( list.Remove( new NbtInt( 5 ) ) );
            Assert.IsTrue( list.Remove( sameTags[0] ) );
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


        [Test]
        public void Serializing() {
            NbtFile writtenFile = new NbtFile();
            NbtFile readFile = new NbtFile();
            const string fileName = "TestFiles/NbtListType.nbt";
            const NbtTagType expectedListType = NbtTagType.Int;
            const int elements = 10;

            // construct nbt file
            writtenFile.RootTag = new NbtCompound( "ListTypeTest" );
            NbtList writtenList = new NbtList( "Entities", null, expectedListType );
            for( int i = 0; i < elements; i++ ) {
                writtenList.Add( new NbtInt( i ) );
            }
            writtenFile.RootTag.Add( writtenList );

            // test saving
            writtenFile.SaveFile( fileName, NbtCompression.GZip );

            // test loading
            readFile.LoadFile( fileName );

            // check contents of loaded file
            Assert.NotNull( readFile.RootTag );
            Assert.IsInstanceOf<NbtList>( readFile.RootTag["Entities"] );
            NbtList readList = (NbtList)readFile.RootTag["Entities"];
            Assert.AreEqual( readList.ListType, writtenList.ListType );
            Assert.AreEqual( readList.Count, writtenList.Count );

            // check contents of loaded list
            for( int i = 0; i < elements; i++ ) {
                Assert.AreEqual( readList.Get<NbtInt>( i ).Value, writtenList.Get<NbtInt>( i ).Value );
            }
        }
    }
}