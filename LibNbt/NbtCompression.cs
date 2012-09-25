namespace LibNbt {
    public enum NbtCompression {
        /// <summary> Automatically detect file compression. </summary>
        AutoDetect,

        /// <summary> No compression. </summary>
        None,

        /// <summary> GZip compression (default). </summary>
        GZip,

        /// <summary> ZLib compression (not implemented). </summary>
        ZLib
    }
}
