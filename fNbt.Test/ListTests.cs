using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class ListTests {
        [Test]
        public void InterfaceImplementation() {
            // prepare our test lists
            var referenceList = new List<NbtTag> {
                new NbtInt(1),
                new NbtInt(2),
                new NbtInt(3)
            };
            var testTag = new NbtInt(4);
            var originalList = new NbtList(referenceList);

            // check IList implementations
            IList iList = originalList;
            CollectionAssert.AreEqual(referenceList, iList);

            // check IList.Add
            referenceList.Add(testTag);
            iList.Add(testTag);
            CollectionAssert.AreEqual(referenceList, iList);

            // check IList.IndexOf
            Assert.AreEqual(referenceList.IndexOf(testTag), iList.IndexOf(testTag));

            // check IList.Contains
            Assert.IsTrue(iList.Contains(testTag));

            // check IList.Remove
            iList.Remove(testTag);
            Assert.IsFalse(iList.Contains(testTag));

            // check IList.Insert
            iList.Insert(0, testTag);
            Assert.AreEqual(iList.IndexOf(testTag), 0);

            // check IList.RemoveAt
            iList.RemoveAt(0);
            Assert.IsFalse(iList.Contains(testTag));

            // check misc IList properties
            Assert.IsFalse(iList.IsFixedSize);
            Assert.IsFalse(iList.IsReadOnly);
            Assert.IsFalse(iList.IsSynchronized);
            Assert.NotNull(iList.SyncRoot);

            // check IList.CopyTo
            var exportTest = new NbtInt[iList.Count];
            iList.CopyTo(exportTest, 0);
            CollectionAssert.AreEqual(iList, exportTest);

            // check IList.this[int]
            for (int i = 0; i < iList.Count; i++) {
                Assert.AreEqual(iList[i], originalList[i]);
                iList[i] = new NbtInt(i);
            }

            // check IList<NbtTag>.IsReadOnly
            IList<NbtTag> iGenericList = originalList;
            Assert.IsFalse(iGenericList.IsReadOnly);

            // check IList.Clear
            iList.Clear();
            Assert.AreEqual(iList.Count, 0);
            Assert.AreEqual(iList.IndexOf(testTag), -1);
        }


        [Test]
        public void IndexerTest() {
            var secondList = new NbtList {
                new NbtByte()
            };

            var testList = new NbtList();
            // Trying to set an out-of-range element
            Assert.Throws<ArgumentOutOfRangeException>(() => testList[0] = new NbtByte(1));

            // Make sure that setting did not affect ListType
            Assert.AreEqual(testList.ListType, NbtTagType.Unknown);
            Assert.AreEqual(testList.Count, 0);
            testList.Add(new NbtByte(1));

            // set a tag to null
            Assert.Throws<ArgumentNullException>(() => testList[0] = null);

            // set a tag to itself
            Assert.Throws<ArgumentException>(() => testList[0] = testList);

            // give a named tag where an unnamed tag was expected
            Assert.Throws<ArgumentException>(() => testList[0] = new NbtByte("NamedTag"));

            // give an unnamed tag that already has a parent
            Assert.Throws<ArgumentException>(() => testList[0] = secondList[0]);
        }


        [Test]
        public void InitializingListFromCollection() {
            // auto-detecting list type
            var test1 = new NbtList("Test1", new NbtTag[] {
                new NbtInt(1),
                new NbtInt(2),
                new NbtInt(3)
            });
            Assert.AreEqual(test1.ListType, NbtTagType.Int);

            // correct explicitly-given list type
            Assert.DoesNotThrow(() => new NbtList("Test2", new NbtTag[] {
                new NbtInt(1),
                new NbtInt(2),
                new NbtInt(3)
            }, NbtTagType.Int));

            // wrong explicitly-given list type
            Assert.Throws<ArgumentException>(() => new NbtList("Test3", new NbtTag[] {
                new NbtInt(1),
                new NbtInt(2),
                new NbtInt(3)
            }, NbtTagType.Float));

            // auto-detecting mixed list given
            Assert.Throws<ArgumentException>(() => new NbtList("Test4", new NbtTag[] {
                new NbtFloat(1),
                new NbtByte(2),
                new NbtInt(3)
            }));

            // using AddRange
            Assert.DoesNotThrow(() => new NbtList().AddRange(new NbtTag[] {
                new NbtInt(1),
                new NbtInt(2),
                new NbtInt(3)
            }));
        }


        [Test]
        public void ManipulatingList() {
            var sameTags = new NbtTag[] {
                new NbtInt(0),
                new NbtInt(1),
                new NbtInt(2)
            };

            var list = new NbtList("Test1", sameTags);

            // testing enumerator, indexer, Contains, and IndexOf
            int j = 0;
            foreach (NbtTag tag in list) {
                Assert.IsTrue(list.Contains(sameTags[j]));
                Assert.AreEqual(tag, sameTags[j]);
                Assert.AreEqual(list.IndexOf(tag), j);
                j++;
            }

            // adding an item of correct type
            list.Add(new NbtInt(3));
            list.Insert(3, new NbtInt(4));

            // adding an item of wrong type
            Assert.Throws<ArgumentException>(() => list.Add(new NbtString()));
            Assert.Throws<ArgumentException>(() => list.Insert(3, new NbtString()));

            // testing array contents
            for (int i = 0; i < sameTags.Length; i++) {
                Assert.AreSame(sameTags[i], list[i]);
                Assert.AreEqual(((NbtInt)list[i]).Value, i);
            }

            // test removal
            Assert.IsFalse(list.Remove(new NbtInt(5)));
            Assert.IsTrue(list.Remove(sameTags[0]));
            list.RemoveAt(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(10));

            // Test some failure scenarios for Add:
            // adding a list to itself
            var loopList = new NbtList();
            Assert.AreEqual(loopList.ListType, NbtTagType.Unknown);
            Assert.Throws<ArgumentException>(() => loopList.Add(loopList));

            // adding same tag to multiple lists
            Assert.Throws<ArgumentException>(() => loopList.Add(list[0]));

            // adding null tag
            Assert.Throws<ArgumentNullException>(() => loopList.Add(null));

            // make sure that all those failed adds didn't affect the tag
            Assert.AreEqual(loopList.Count, 0);
            Assert.AreEqual(loopList.ListType, NbtTagType.Unknown);

            // try creating a list with invalid tag type
            Assert.Throws<ArgumentOutOfRangeException>(() => new NbtList(NbtTagType.End));
        }


        [Test]
        public void ChangingListTagType() {
            var list = new NbtList();

            // changing list type to an out-of-range type
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ListType = (NbtTagType)200);

            // changing type of an empty list
            Assert.DoesNotThrow(() => list.ListType = NbtTagType.Unknown);

            list.Add(new NbtInt());

            // setting correct type for a non-empty list
            Assert.DoesNotThrow(() => list.ListType = NbtTagType.Int);

            // changing list type to an incorrect type
            Assert.Throws<ArgumentException>(() => list.ListType = NbtTagType.Short);
        }


        [Test]
        public void SerializingWithoutListType() {
            var root = new NbtCompound("root") {
                new NbtList("list")
            };
            var file = new NbtFile(root);

            using (var ms = new MemoryStream()) {
                // list should throw NbtFormatException, because its ListType is Unknown
                Assert.Throws<NbtFormatException>(() => file.SaveToStream(ms, NbtCompression.None));
            }
        }


        [Test]
        public void Serializing1() {
            // check the basics of saving/loading
            const NbtTagType expectedListType = NbtTagType.Int;
            const int elements = 10;

            // construct nbt file
            var writtenFile = new NbtFile(new NbtCompound("ListTypeTest"));
            var writtenList = new NbtList("Entities", null, expectedListType);
            for (int i = 0; i < elements; i++) {
                writtenList.Add(new NbtInt(i));
            }
            writtenFile.RootTag.Add(writtenList);

            // test saving
            byte[] data = writtenFile.SaveToBuffer(NbtCompression.None);

            // test loading
            var readFile = new NbtFile();
            int bytesRead = readFile.LoadFromBuffer(data, 0, data.Length, NbtCompression.None);
            Assert.AreEqual(bytesRead, data.Length);

            // check contents of loaded file
            Assert.NotNull(readFile.RootTag);
            Assert.IsInstanceOf<NbtList>(readFile.RootTag["Entities"]);
            var readList = (NbtList)readFile.RootTag["Entities"];
            Assert.AreEqual(readList.ListType, writtenList.ListType);
            Assert.AreEqual(readList.Count, writtenList.Count);

            // check .ToArray
            CollectionAssert.AreEquivalent(readList, readList.ToArray());
            CollectionAssert.AreEquivalent(readList, readList.ToArray<NbtInt>());

            // check contents of loaded list
            for (int i = 0; i < elements; i++) {
                Assert.AreEqual(readList.Get<NbtInt>(i).Value, writtenList.Get<NbtInt>(i).Value);
            }
        }


        [Test]
        public void Serializing2() {
            // check saving/loading lists of all possible value types
            var testFile = new NbtFile(TestFiles.MakeListTest());
            byte[] buffer = testFile.SaveToBuffer(NbtCompression.None);
            int bytesRead = testFile.LoadFromBuffer(buffer, 0, buffer.Length, NbtCompression.None);
            Assert.AreEqual(bytesRead, buffer.Length);
        }


        [Test]
        public void NestedListAndCompoundTest() {
            byte[] data;
            {
                var root = new NbtCompound("Root");
                var outerList = new NbtList("OuterList", NbtTagType.Compound);
                var outerCompound = new NbtCompound();
                var innerList = new NbtList("InnerList", NbtTagType.Compound);
                var innerCompound = new NbtCompound();

                innerList.Add(innerCompound);
                outerCompound.Add(innerList);
                outerList.Add(outerCompound);
                root.Add(outerList);

                var file = new NbtFile(root);
                data = file.SaveToBuffer(NbtCompression.None);
            }
            {
                var file = new NbtFile();
                int bytesRead = file.LoadFromBuffer(data, 0, data.Length, NbtCompression.None);
                Assert.AreEqual(bytesRead, data.Length);
                Assert.AreEqual(file.RootTag.Get<NbtList>("OuterList").Count, 1);
                Assert.AreEqual(file.RootTag.Get<NbtList>("OuterList").Get<NbtCompound>(0).Name, null);
                Assert.AreEqual(
                    file.RootTag.Get<NbtList>("OuterList").Get<NbtCompound>(0).Get<NbtList>("InnerList").Count,
                    1);
                Assert.AreEqual(
                    file.RootTag.Get<NbtList>("OuterList")
                        .Get<NbtCompound>(0)
                        .Get<NbtList>("InnerList")
                        .Get<NbtCompound>(0)
                        .Name,
                    null);
            }
        }
    }
}
