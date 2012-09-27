namespace LibNbt {
    /// <summary> NBT tag that holds a single value. </summary>
    /// <typeparam name="T"> Type of the value. </typeparam>
    public interface INbtTagValue<T> {
        /// <summary> Value/payload of this tag. </summary>
        T Value { get; set; }
    }
}