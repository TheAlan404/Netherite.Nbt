namespace fNbt {
    struct NbtReaderDepthState {
        public string ParentName;
        public NbtTagType ParentTagType;
        public int TagLength;
        public int ListIndex;
    }
}