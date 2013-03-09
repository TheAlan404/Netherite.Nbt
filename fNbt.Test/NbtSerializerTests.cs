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
            DataTypeTestClass derp = (DataTypeTestClass)serializer.Deserialize( testTag, typeof( DataTypeTestClass ) );
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
    }
}