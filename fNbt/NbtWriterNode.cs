using System.Collections.Generic;

namespace fNbt {
    sealed class NbtWriterNode {
        public NbtTagType ParentType;
        public NbtTagType ListType;
        public int ListSize;
        public int ListIndex;
        public Dictionary<string, bool> UsedNames;
    }
}