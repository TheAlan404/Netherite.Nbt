namespace fNbt.Serialization {
    /// <summary> Defines how null values should be treated. </summary>
    public enum NullPolicy {
        Default = 0,
        Error = 0,
        Ignore = 1,
        InsertDefault = 2
    }
}