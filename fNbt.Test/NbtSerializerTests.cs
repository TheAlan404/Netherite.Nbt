using System.Collections;
using System.Reflection;
using fNbt.Serialization;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    internal class NbtSerializerTests {
        [Test]
        public void ValueTest() {
            TestFiles.ValueTestClass testObject = TestFiles.MakeValueTestObject();
            var serializer = new NbtSerializer(testObject.GetType());

            NbtTag serializedTag = serializer.Serialize(testObject, "root");

            Assert.IsAssignableFrom(typeof(NbtCompound), serializedTag);

            TestFiles.AssertValueTest(new NbtFile((NbtCompound)serializedTag));
        }


        [Test]
        public void ConversionTest() {
            var defaultObject = new TestFiles.SerializerConversionClass();
            AssertSerializerRoundTrip(new NbtSerializer(defaultObject.GetType()), defaultObject);
        }


        // Serializes object to NBT, restores it, and compares properties to the original.
        static void AssertSerializerRoundTrip(NbtSerializer serializer, object originalObject) {
            NbtTag serializedObject = serializer.Serialize(originalObject);
            var retrievedObject = (TestFiles.SerializerConversionClass)serializer.Deserialize(serializedObject);
            AssertPropertiesEqual(originalObject, retrievedObject);
        }


        // Checks whether two objects have exact same properties
        static void AssertPropertiesEqual<T>(T actual, T expected) {
            PropertyInfo[] properties = expected.GetType().GetProperties();
            foreach (PropertyInfo property in properties) {
                object expectedValue = property.GetValue(expected, null);
                object actualValue = property.GetValue(actual, null);

                var list = actualValue as IList;
                if (list != null) {
                    CollectionAssert.AreEqual(list, (IList)expectedValue);
                } else {
                    Assert.AreEqual(actualValue, expectedValue);
                }
            }
        }
    }
}
