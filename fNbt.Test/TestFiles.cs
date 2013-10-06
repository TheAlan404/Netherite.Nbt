using System.IO;

namespace fNbt.Test {
    static class TestFiles {
        public const string DirName = "TestFiles";
        public static readonly string Small = Path.Combine( DirName, "test.nbt" );
        public static readonly string SmallGZip = Path.Combine( DirName, "test.nbt.gz" );
        public static readonly string SmallZLib = Path.Combine( DirName, "test.nbt.z" );
        public static readonly string Big = Path.Combine( DirName, "bigtest.nbt" );
        public static readonly string BigGZip = Path.Combine( DirName, "bigtest.nbt.gz" );
        public static readonly string BigZLib = Path.Combine( DirName, "bigtest.nbt.z" );
    }
}
