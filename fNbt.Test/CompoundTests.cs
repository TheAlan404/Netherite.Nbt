using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class CompoundTests {
        const string TempDir = "TestTemp";


        [SetUp]
        public void CompoundTestsSetup() {
            Directory.CreateDirectory( TempDir );
        }


        [Test]
        public void InitializingCompoundFromCollectionTest() {
            NbtTag[] allNamed = {
                new NbtShort( "allNamed1", 1 ),
                new NbtLong( "allNamed2", 2 ),
                new NbtInt( "allNamed3", 3 )
            };

            NbtTag[] someUnnamed = {
                new NbtInt( "someUnnamed1", 1 ),
                new NbtInt( 2 ),
                new NbtInt( "someUnnamed3", 3 )
            };

            NbtTag[] someNull = {
                new NbtInt( "someNull1", 1 ),
                null,
                new NbtInt( "someNull3", 3 )
            };

            NbtTag[] dupeNames = {
                new NbtInt( "dupeNames1", 1 ),
                new NbtInt( "dupeNames2", 2 ),
                new NbtInt( "dupeNames1", 3 )
            };

            // null collection, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "nullTest", null ) );

            // proper initialization
            NbtCompound allNamedTest = null;
            Assert.DoesNotThrow( () => allNamedTest = new NbtCompound( "allNamedTest", allNamed ) );
            CollectionAssert.AreEquivalent( allNamed, allNamedTest );

            // some tags are unnamed, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "someUnnamedTest", someUnnamed ) );

            // some tags are null, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "someNullTest", someNull ) );

            // some tags have same names, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "dupeNamesTest", dupeNames ) );
        }


        [Test]
        public void GettersAndSetters() {
            NbtCompound parent = new NbtCompound( "Parent" );
            NbtCompound child = new NbtCompound( "Child" );
            NbtCompound nestedChild = new NbtCompound( "NestedChild" );
            NbtList childList = new NbtList( "ChildList" );
            NbtList nestedChildList = new NbtList( "NestedChildList" );
            childList.Add( new NbtInt( 1 ) );
            NbtInt nestedInt = new NbtInt( 1 );
            nestedChildList.Add( nestedInt );
            parent.Add( child );
            parent.Add( childList );
            child.Add( nestedChild );
            child.Add( nestedChildList );

            // Accessing nested compound tags using indexers
            Assert.AreEqual( parent["Child"]["NestedChild"], nestedChild );
            Assert.AreEqual( parent["Child"]["NestedChildList"], nestedChildList );
            Assert.AreEqual( parent["Child"]["NestedChildList"][0], nestedInt );

            // Accessing nested compound tags using Get<T>
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtCompound>( "NestedChild" ), nestedChild );
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtList>( "NestedChildList" ), nestedChildList );
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtList>( "NestedChildList" )[0], nestedInt );

            // Accessing with Get<T> and an invalid given type
            Assert.Throws<InvalidCastException>( () => parent.Get<NbtInt>( "Child" ) );

            // Trying to use integer indexers on non-NbtList tags
            Assert.Throws<InvalidOperationException>( () => parent[0] = nestedInt );
            Assert.Throws<InvalidOperationException>( () => nestedInt[0] = nestedInt );

            // Trying to use string indexers on non-NbtCompound tags
            Assert.Throws<InvalidOperationException>( () => childList["test"] = nestedInt );
            Assert.Throws<InvalidOperationException>( () => nestedInt["test"] = nestedInt );

            // Trying to get a non-existent element by name
            Assert.IsNull( parent.Get<NbtTag>( "NonExistentTag" ) );
            Assert.IsNull( parent["NonExistentTag"] );

            // Null indices on NbtCompound
            Assert.Throws<ArgumentNullException>( () => parent.Get<NbtTag>( null ) );
            Assert.Throws<ArgumentNullException>( () => parent[null] = new NbtInt( 1 ) );
            Assert.Throws<ArgumentNullException>( () => nestedInt = (NbtInt)parent[null] );

            // Out-of-range indices on NbtList
            Assert.Throws<ArgumentOutOfRangeException>( () => nestedInt = (NbtInt)childList[-1] );
            Assert.Throws<ArgumentOutOfRangeException>( () => childList[-1] = new NbtInt( 1 ) );
            Assert.Throws<ArgumentOutOfRangeException>( () => nestedInt = childList.Get<NbtInt>( -1 ) );
            Assert.Throws<ArgumentOutOfRangeException>( () => nestedInt = (NbtInt)childList[childList.Count] );
            Assert.Throws<ArgumentOutOfRangeException>( () => nestedInt = childList.Get<NbtInt>( childList.Count ) );
        }


        [Test]
        public void Renaming() {
            NbtCompound compound = new NbtCompound();
            compound.Add( new NbtInt( "SameName", 1 ) );
            var tagToRename = new NbtInt( "DifferentName", 1 );
            compound.Add( tagToRename );
            Assert.DoesNotThrow( () => tagToRename.Name = "SomeOtherName" );
            Assert.Throws<ArgumentException>( () => tagToRename.Name = "SameName" );
            Assert.Throws<ArgumentNullException>( () => tagToRename.Name = null );
        }


        [Test]
        public void AddingAndRemoving() {
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

            // clearing an empty NbtCompound
            Assert.AreEqual( test.Count, 0 );
            test.Clear();

            // re-adding after clearing
            test.Add( foo );
            Assert.AreEqual( test.Count, 1 );

            // clearing a non-empty NbtCompound
            test.Clear();
            Assert.AreEqual( test.Count, 0 );
        }


        [Test]
        public void UtilityMethods() {
            NbtTag[] testThings = new NbtTag[] {
                new NbtShort( "Name1", 1 ),
                new NbtInt( "Name2", 2 ),
                new NbtLong( "Name3", 3 )
            };
            NbtCompound compound = new NbtCompound();

            // add range
            compound.AddRange( testThings );

            // add range with duplicates
            Assert.Throws<ArgumentException>( () => compound.AddRange( testThings ) );

            // check ToNameArray and ToArray, which are provided for .NET 2.0 users' convenience
            CollectionAssert.AreEqual( compound.Names, compound.ToNameArray() );
            CollectionAssert.AreEqual( compound.Tags, compound.ToArray() );
        }


        [Test]
        public void InterfaceImplementations() {
            NbtTag[] tagList = {
                new NbtByte( "First", 1 ), new NbtShort( "Second", 2 ), new NbtInt( "Third", 3 ),
                new NbtLong( "Fourth", 4L )
            };

            // test NbtCompound(IEnumerable<NbtTag>) constructor
            NbtCompound comp = new NbtCompound( tagList );

            // test .Names and .Tags collections
            CollectionAssert.AreEquivalent( comp.Names,
                                            new[] {
                                                "First", "Second", "Third", "Fourth"
                                            } );
            CollectionAssert.AreEquivalent( comp.Tags, tagList );

            // test ICollection and ICollection<NbtTag> boilerplate properties
            ICollection<NbtTag> iGenCollection = comp;
            Assert.IsFalse( iGenCollection.IsReadOnly );
            ICollection iCollection = comp;
            Assert.NotNull( iCollection.SyncRoot );
            Assert.IsFalse( iCollection.IsSynchronized );

            // test CopyTo()
            NbtTag[] tags = new NbtTag[iCollection.Count];
            iCollection.CopyTo( tags, 0 );
            CollectionAssert.AreEquivalent( tags, comp );

            // test GetEnumerator()
            List<NbtTag> enumeratedTags = new List<NbtTag>();
            IEnumerator<NbtTag> tagEnum = comp.GetEnumerator();
            while( tagEnum.MoveNext() ) {
                enumeratedTags.Add( tagEnum.Current );
            }
            CollectionAssert.AreEquivalent( enumeratedTags, tagList );
        }


        [TearDown]
        public void CompoundTestsTearDown() {
            if( Directory.Exists( TempDir ) ) {
                Directory.Delete( TempDir, true );
            }
        }
    }
}