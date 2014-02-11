using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fNbt.Serialization;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    internal class NbtCompilerTests {
        [Test]
        public void ValueTest() {
            var serializer = NbtCompiler.GetSerializer<TestFiles.ValueTestClass>();
            TestFiles.ValueTestClass testObject = TestFiles.MakeValueTestObject();
            NbtCompound serializedTag = serializer("root", testObject);
            TestFiles.AssertValueTest(new NbtFile(serializedTag));
        }
    }
}
