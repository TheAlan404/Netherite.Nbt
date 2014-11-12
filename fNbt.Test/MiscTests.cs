using System;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public class MiscTests {
        [Test]
        public void CopyConstructorTest() {
            NbtByte byteTag = new NbtByte("byteTag", 1);
            NbtByte byteTagClone = (NbtByte)byteTag.Clone();
            Assert.AreNotSame(byteTag, byteTagClone);
            Assert.AreEqual(byteTag.Name, byteTagClone.Name);
            Assert.AreEqual(byteTag.Value, byteTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtByte((NbtByte)null));

            NbtByteArray byteArrTag = new NbtByteArray("byteArrTag", new byte[] { 1, 2, 3, 4 });
            NbtByteArray byteArrTagClone = (NbtByteArray)byteArrTag.Clone();
            Assert.AreNotSame(byteArrTag, byteArrTagClone);
            Assert.AreEqual(byteArrTag.Name, byteArrTagClone.Name);
            Assert.AreNotSame(byteArrTag.Value, byteArrTagClone.Value);
            CollectionAssert.AreEqual(byteArrTag.Value, byteArrTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtByteArray((NbtByteArray)null));

            NbtCompound compTag = new NbtCompound("compTag", new NbtTag[] { new NbtByte("innerTag", 1) });
            NbtCompound compTagClone = (NbtCompound)compTag.Clone();
            Assert.AreNotSame(compTag, compTagClone);
            Assert.AreEqual(compTag.Name, compTagClone.Name);
            Assert.AreNotSame(compTag["innerTag"], compTagClone["innerTag"]);
            Assert.AreEqual(compTag["innerTag"].Name, compTagClone["innerTag"].Name);
            Assert.AreEqual(compTag["innerTag"].ByteValue, compTagClone["innerTag"].ByteValue);
            Assert.Throws<ArgumentNullException>(() => new NbtCompound((NbtCompound)null));

            NbtDouble doubleTag = new NbtDouble("doubleTag", 1);
            NbtDouble doubleTagClone = (NbtDouble)doubleTag.Clone();
            Assert.AreNotSame(doubleTag, doubleTagClone);
            Assert.AreEqual(doubleTag.Name, doubleTagClone.Name);
            Assert.AreEqual(doubleTag.Value, doubleTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtDouble((NbtDouble)null));

            NbtFloat floatTag = new NbtFloat("floatTag", 1);
            NbtFloat floatTagClone = (NbtFloat)floatTag.Clone();
            Assert.AreNotSame(floatTag, floatTagClone);
            Assert.AreEqual(floatTag.Name, floatTagClone.Name);
            Assert.AreEqual(floatTag.Value, floatTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtFloat((NbtFloat)null));

            NbtInt intTag = new NbtInt("intTag", 1);
            NbtInt intTagClone = (NbtInt)intTag.Clone();
            Assert.AreNotSame(intTag, intTagClone);
            Assert.AreEqual(intTag.Name, intTagClone.Name);
            Assert.AreEqual(intTag.Value, intTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtInt((NbtInt)null));

            NbtIntArray intArrTag = new NbtIntArray("intArrTag", new[] { 1, 2, 3, 4 });
            NbtIntArray intArrTagClone = (NbtIntArray)intArrTag.Clone();
            Assert.AreNotSame(intArrTag, intArrTagClone);
            Assert.AreEqual(intArrTag.Name, intArrTagClone.Name);
            Assert.AreNotSame(intArrTag.Value, intArrTagClone.Value);
            CollectionAssert.AreEqual(intArrTag.Value, intArrTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtIntArray((NbtIntArray)null));

            NbtList listTag = new NbtList("listTag", new NbtTag[] { new NbtByte(1) });
            NbtList listTagClone = (NbtList)listTag.Clone();
            Assert.AreNotSame(listTag, listTagClone);
            Assert.AreEqual(listTag.Name, listTagClone.Name);
            Assert.AreNotSame(listTag[0], listTagClone[0]);
            Assert.AreEqual(listTag[0].ByteValue, listTagClone[0].ByteValue);
            Assert.Throws<ArgumentNullException>(() => new NbtList((NbtList)null));

            NbtLong longTag = new NbtLong("longTag", 1);
            NbtLong longTagClone = (NbtLong)longTag.Clone();
            Assert.AreNotSame(longTag, longTagClone);
            Assert.AreEqual(longTag.Name, longTagClone.Name);
            Assert.AreEqual(longTag.Value, longTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtLong((NbtLong)null));

            NbtShort shortTag = new NbtShort("shortTag", 1);
            NbtShort shortTagClone = (NbtShort)shortTag.Clone();
            Assert.AreNotSame(shortTag, shortTagClone);
            Assert.AreEqual(shortTag.Name, shortTagClone.Name);
            Assert.AreEqual(shortTag.Value, shortTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtShort((NbtShort)null));

            NbtString stringTag = new NbtString("stringTag", "foo");
            NbtString stringTagClone = (NbtString)stringTag.Clone();
            Assert.AreNotSame(stringTag, stringTagClone);
            Assert.AreEqual(stringTag.Name, stringTagClone.Name);
            Assert.AreEqual(stringTag.Value, stringTagClone.Value);
            Assert.Throws<ArgumentNullException>(() => new NbtString((NbtString)null));
        }


        [Test]
        public void ByteArrayIndexerTest() {
            // test getting/settings values of byte array tag via indexer
            var byteArray = new NbtByteArray("Test");
            CollectionAssert.AreEqual(byteArray.Value, new byte[0]);
            byteArray.Value = new byte[] {
                1, 2, 3
            };
            Assert.AreEqual(byteArray[0], 1);
            Assert.AreEqual(byteArray[1], 2);
            Assert.AreEqual(byteArray[2], 3);
            byteArray[0] = 4;
            Assert.AreEqual(byteArray[0], 4);
        }


        [Test]
        public void IntArrayIndexerTest() {
            // test getting/settings values of int array tag via indexer
            var byteArray = new NbtIntArray("Test");
            CollectionAssert.AreEqual(byteArray.Value, new int[0]);
            byteArray.Value = new int[] {
                1, 2000, -3000000
            };
            Assert.AreEqual(byteArray[0], 1);
            Assert.AreEqual(byteArray[1], 2000);
            Assert.AreEqual(byteArray[2], -3000000);
            byteArray[0] = 4;
            Assert.AreEqual(byteArray[0], 4);
        }


        [Test]
        public void DefaultValueTest() {
            // test default values of all value tags
            Assert.AreEqual(new NbtByte("test").Value, 0);
            CollectionAssert.AreEqual(new NbtByteArray("test").Value, new byte[0]);
            Assert.AreEqual(new NbtDouble("test").Value, 0d);
            Assert.AreEqual(new NbtFloat("test").Value, 0f);
            Assert.AreEqual(new NbtInt("test").Value, 0);
            CollectionAssert.AreEqual(new NbtIntArray("test").Value, new int[0]);
            Assert.AreEqual(new NbtLong("test").Value, 0L);
            Assert.AreEqual(new NbtShort("test").Value, 0);
            Assert.AreEqual(new NbtString().Value, "");
        }


        [Test]
        public void NullValueTest() {
            Assert.Throws<ArgumentNullException>(() => new NbtByteArray().Value = null);
            Assert.Throws<ArgumentNullException>(() => new NbtIntArray().Value = null);
            Assert.Throws<ArgumentNullException>(() => new NbtString().Value = null);
        }


        [Test]
        public void PathTest() {
            // test NbtTag.Path property
            var testComp = new NbtCompound {
                new NbtCompound("Compound") {
                    new NbtCompound("InsideCompound")
                },
                new NbtList("List") {
                    new NbtCompound {
                        new NbtInt("InsideCompoundAndList")
                    }
                }
            };

            // parent-less tag with no name has empty string for a path
            Assert.AreEqual(testComp.Path, "");
            Assert.AreEqual(testComp["Compound"].Path, ".Compound");
            Assert.AreEqual(testComp["Compound"]["InsideCompound"].Path, ".Compound.InsideCompound");
            Assert.AreEqual(testComp["List"].Path, ".List");

            // tags inside lists have no name, but they do have an index
            Assert.AreEqual(testComp["List"][0].Path, ".List[0]");
            Assert.AreEqual(testComp["List"][0]["InsideCompoundAndList"].Path, ".List[0].InsideCompoundAndList");
        }
    }
}
