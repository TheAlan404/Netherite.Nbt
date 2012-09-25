namespace LibNbt.Tags {
    interface INbtTagValue<T> {
        T Value { get; set; }
    }
}