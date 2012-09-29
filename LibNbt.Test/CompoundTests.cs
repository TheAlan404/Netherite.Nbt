using System;
using System.IO;
using NUnit.Framework;

namespace LibNbt.Test {
    [TestFixture]
    public sealed class CompoundTests {

        const string TempDir = "TestTemp";


        [SetUp]
        public void CompoundTestsSetup() {
            Directory.CreateDirectory( TempDir );
        }


        [Test]
        public void InitializingCompoundFromCollection() {
            NbtTag[] allNamed = new NbtTag[] {
                new NbtShort( "allNamed1", 1 ),
                new NbtLong( "allNamed2", 2 ),
                new NbtInt( "allNamed3", 3 )
            };

            NbtTag[] someUnnamed = new NbtTag[] {
                new NbtInt( "someUnnamed1", 1 ),
                new NbtInt( 2 ),
                new NbtInt( "someUnnamed3", 3 )
            };

            NbtTag[] someNull = new NbtTag[] {
                new NbtInt( "someNull1", 1 ),
                null,
                new NbtInt( "someNull3", 3 )
            };

            NbtTag[] dupeNames = new NbtTag[] {
                new NbtInt( "dupeNames1", 1 ),
                new NbtInt( "dupeNames2", 2 ),
                new NbtInt( "dupeNames1", 3 )
            };

            // null collection, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "nullTest", null ) );

            // proper initialization
            Assert.DoesNotThrow( () => new NbtCompound( "allNamedTest", allNamed ) );
            CollectionAssert.AreEquivalent( allNamed, new NbtCompound( "allNamedTest", allNamed ) );

            // some tags are unnamed, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "someUnnamedTest", someUnnamed ) );

            // some tags are null, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "someNullTest", someNull ) );

            // some tags have same names, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "dupeNamesTest", dupeNames ) );
        }


        [Test]
        public void ManipulatingCompounds() {
            NbtCompound test = new NbtCompound();

            NbtInt foo =  new NbtInt( "Foo" );

            test.Add( foo );

            // adding duplicate object
            Assert.Throws<ArgumentException>( () => test.Add( foo ) );

            // adding duplicate name
            Assert.Throws<ArgumentException>( () => test.Add( new NbtByte( "Foo" ) ) );

            // adding unnamed tag
            Assert.Throws<ArgumentException>( () => test.Add( new NbtInt() ) );

            // adding null
            Assert.Throws<ArgumentNullException>( () => test.Add( null ) );

            // contains existing name
            Assert.IsTrue( test.Contains( "Foo" ) );

            // contains existing object
            Assert.IsTrue( test.Contains( foo ) );

            // contains non-existent name
            Assert.IsFalse( test.Contains( "Bar" ) );

            // contains existing name / different object
            Assert.IsFalse( test.Contains( new NbtInt( "Foo" ) ) );

            // removing non-existent name
            Assert.IsFalse( test.Remove( "Bar" ) );

            // removing existing name
            Assert.IsTrue( test.Remove( "Foo" ) );

            // removing non-existent name
            Assert.IsFalse( test.Remove( "Foo" ) );

            // re-adding object
            test.Add( foo );

            // removing existing object
            Assert.IsTrue( test.Remove( foo ) );
        }


        [TearDown]
        public void CompoundTestsTearDown() {
            if( Directory.Exists( TempDir ) ) {
                foreach( var file in Directory.GetFiles( TempDir ) ) {
                    File.Delete( file );
                }
                Directory.Delete( TempDir );
            }
        }
    }
}