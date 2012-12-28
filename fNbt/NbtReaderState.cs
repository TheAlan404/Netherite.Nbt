namespace fNbt {
    struct NbtReaderState {
        public string ParentName;
        public NbtTagType ParentTagType;
        public int TagLength;
        public int ListIndex;
    }
}