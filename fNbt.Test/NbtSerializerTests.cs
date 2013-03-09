using System.ComponentModel;
using NUnit.Framework;
using fNbt.Serialization;

namespace fNbt.Test {
    [TestFixture]
    public sealed class NbtSerializerTest {
        [Test]
        public void DataTypeTest() {
            DataTypeTestClass testObject = new DataTypeTestClass {
                BoolProperty = true,
                ByteArrayProperty = new byte[] { 1, 2, 3 },
                ByteProperty = 4,
                CharProperty = '5',
                IntArrayProperty = new[] { 6, 7, 8 },
                IntProperty = -9,
                LongProperty = -10,
                SByteProperty = -11,
                ShortProperty = -12,
                StringProperty = "13",
                UIntProperty = 14,
                ULongProperty = 15,
                UShortProperty = 16
            };
            NbtSerializer serializer = new NbtSerializer();
            NbtTag testTag = serializer.Serialize( testObject, "testTag" );
            DataTypeTestClass remadeObject = (DataTypeTestClass)serializer.Deserialize( testTag, typeof( DataTypeTestClass ) );
            Assert.IsNotNull( remadeObject );

            Assert.AreEqual( testObject.BoolProperty, remadeObject.BoolProperty );
            Assert.AreEqual( testObject.ByteProperty, remadeObject.ByteProperty );
            Assert.AreEqual( testObject.SByteProperty, remadeObject.SByteProperty );
            Assert.AreEqual( testObject.ShortProperty, remadeObject.ShortProperty );
            Assert.AreEqual( testObject.UShortProperty, remadeObject.UShortProperty );
            Assert.AreEqual( testObject.CharProperty, remadeObject.CharProperty );
            Assert.AreEqual( testObject.IntProperty, remadeObject.IntProperty );
            Assert.AreEqual( testObject.UIntProperty, remadeObject.UIntProperty );
            Assert.AreEqual( testObject.LongProperty, remadeObject.LongProperty );
            Assert.AreEqual( testObject.ULongProperty, remadeObject.ULongProperty );
            Assert.AreEqual( testObject.StringProperty, remadeObject.StringProperty );
            CollectionAssert.AreEqual( testObject.ByteArrayProperty, testObject.ByteArrayProperty );
            CollectionAssert.AreEqual( testObject.IntArrayProperty, testObject.IntArrayProperty );
        }


        class DataTypeTestClass {
            public bool BoolProperty { get; set; }
            public byte ByteProperty { get; set; }
            public sbyte SByteProperty { get; set; }
            public short ShortProperty { get; set; }
            public ushort UShortProperty { get; set; }
            public char CharProperty { get; set; }
            public int IntProperty { get; set; }
            public uint UIntProperty { get; set; }
            public long LongProperty { get; set; }
            public ulong ULongProperty { get; set; }
            public string StringProperty { get; set; }
            public byte[] ByteArrayProperty { get; set; }
            public int[] IntArrayProperty { get; set; }
        }


        [Test]
        public void AttributeTest() {
            AttributeTestClass testObject = new AttributeTestClass {
                NormalProp = "1",
                RenamedProp = "2",
                IgnoredProp = "3",
                DefaultProp = null
            };

            NbtSerializer serializer = new NbtSerializer();
            NbtCompound testTag = (NbtCompound)serializer.Serialize( testObject, "testTag" );

            Assert.IsTrue( testTag.Contains( "NormalProp" ) );
            Assert.IsTrue( testTag.Contains( "DifferentName" ) );
            Assert.IsFalse( testTag.Contains( "RenamedProp" ) );
            Assert.IsFalse( testTag.Contains( "IgnoredProp" ) );
            Assert.IsFalse( testTag.Contains( "DefaultProp" ) );

            AttributeTestClass remadeObject = (AttributeTestClass)serializer.Deserialize( testTag, typeof( AttributeTestClass ) );
            Assert.IsNotNull( remadeObject );

            Assert.AreEqual( remadeObject.NormalProp, "1" );
            Assert.AreEqual( remadeObject.RenamedProp, "2" );
            Assert.AreEqual( remadeObject.IgnoredProp, null );
            Assert.AreEqual( remadeObject.DefaultProp, "4" );
        }


        class AttributeTestClass {
            public string NormalProp { get; set; }

            [TagName("DifferentName")]
            public string RenamedProp { get; set; }

            [DefaultValue("4")]
            public string DefaultProp { get; set; }

            [NbtIgnore]
            public string IgnoredProp { get; set; }
        }
    }
}