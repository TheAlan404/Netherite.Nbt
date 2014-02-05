namespace fNbt.Serialization {
    public delegate NbtCompound NbtSerialize<T>(string tagName, T value);
}