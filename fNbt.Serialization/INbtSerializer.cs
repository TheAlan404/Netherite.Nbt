namespace fNbt.Serialization {
    public interface INbtSerializer {
        NbtTag MakeTag(object obj);
        object MakeObject(NbtTag tag);
    }
}